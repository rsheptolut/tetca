using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
using Tetca.ActivityDetectors;
using Tetca.Helpers;
using Tetca.Notifiers;
using Tetca.Notifiers.Speech;
using Microsoft.Extensions.Logging;
using System.DirectoryServices.ActiveDirectory;

namespace Tetca.Logic
{
    /// <summary>
    /// The MainLoop class is the core of the application, responsible for managing activity detection, break reminders, and escalation notifications.
    /// It continuously monitors user activity and enforces work-break balance based on configurable settings.
    /// </summary>
    public class MainLoop(WorkRecorder workRecorder, DeliberateActivityFilter deliberateActivityFilter, HumanEscalator humanEscalator, CallDetector callDetector, InputDetector inputDetector, ISpeech speech, Settings settings, Dispatcher dispatcher, ICurrentTime currentTime, ILogger<MainLoop> logger, DoNotDisturb doNotDisturb) : IDisposable
    {
        private readonly Stopwatch stopwatch = new();
        private readonly AutoResetEvent UpdateThreadWaiter = new AutoResetEvent(false);
        private Thread MainLoopThread;
        private TimeSpan TimePassedDuringWait;
        private bool disposedValue;

        /// <summary>
        /// Event triggered after each activity check is performed.
        /// </summary>
        public event EventHandler ActivityCheckPerformed;

        /// <summary>
        /// Event triggered when a break is suggested to the user.
        /// </summary>
        public event EventHandler BreakSuggested;

        /// <summary>
        /// Gets the time until the next activity check is scheduled.
        /// </summary>
        private TimeSpan TimeUntilNextCheck
        {
            get
            {
                var times = new List<TimeSpan>();
                times.Add(settings.MaxCheckCadence);
                times.Add(this.WorkSessionReminder.NextTriggerExpected - currentTime.Now);
                times.Add(this.DayWorkReminder.NextTriggerExpected - currentTime.Now);
                times.Add(this.IdleTimeReminder.NextTriggerExpected - currentTime.Now);
                var smallest = times.Where(t => t > TimeSpan.Zero).Min();

                return smallest.Max(settings.MinCheckCadence);
            }
        }

        /// <summary>
        /// Gets the timestamp of the last break taken by the user.
        /// </summary>
        public DateTime LastBreak { get; protected set; }

        /// <summary>
        /// Gets the timestamp of the last detected user activity.
        /// </summary>
        public DateTime LastActive { get; protected set; }

        /// <summary>
        /// Gets the timestamp of the last activity check
        /// </summary>
        public DateTime LastChecked { get; protected set; }

        /// <summary>
        /// Forces the main loop to recheck activity immediately.
        /// </summary>
        public void Recheck() => this.UpdateThreadWaiter.Set();

        /// <summary>
        /// Gets the duration of the current idle time since the last detected activity.
        /// </summary>
        public TimeSpan CurrentIdleTime => currentTime.Now - this.LastActive;

        /// <summary>
        /// Gets the duration of activity time since the last break.
        /// </summary>
        public TimeSpan ActivityTime => currentTime.Now - this.LastBreak;

        /// <summary>
        /// Gets the total hours worked today, normalized to an 8-hour workday.
        /// </summary>
        public TimeSpan HoursWorkedToday => workRecorder.GetTotalNormie8hWorkedToday();

        /// <summary>
        /// Gets the reminder trigger for work session breaks.
        /// </summary>
        internal ReminderTrigger WorkSessionReminder { get; private set; }

        /// <summary>
        /// Gets the reminder trigger for idle time breaks.
        /// </summary>
        internal ReminderTrigger IdleTimeReminder { get; private set; }

        /// <summary>
        /// Gets the reminder trigger for daily work limits.
        /// </summary>
        internal ReminderTrigger DayWorkReminder { get; private set; }

        /// <summary>
        /// Starts the main loop, initializing reminders and launching the monitoring thread.
        /// </summary>
        public void Start()
        {
            this.Initialize();

            this.MainLoopThread = new Thread(this.Loop);
            this.MainLoopThread.IsBackground = true;
            this.MainLoopThread.Start();
        }

        /// <summary>
        /// Initializes the main loop by setting up reminders, logging activity, and performing startup tasks.
        /// </summary>
        private void Initialize()
        {
            logger.LogDebug("Starting");
            workRecorder.LogActivity();
            this.LastActive = currentTime.Now;
            this.LastBreak = workRecorder.GetLastBreakEnded();
            this.WorkSessionReminder = new ReminderTrigger(currentTime, i => i == 0 ? settings.MaxWorkSession : settings.SubsequentReminderInterval);
            this.IdleTimeReminder = new ReminderTrigger(currentTime, _ => settings.MaxBreak, 3, t => !doNotDisturb.ShouldBeOn(t));
            this.IdleTimeReminder.Reset(3); // don't show idle time reminder right after starting the program
            this.DayWorkReminder = new ReminderTrigger(currentTime, i => i == 0 ? settings.MaxDayWorkTime : settings.DayWorkTimeNotificationInterval);
            this.DayWorkReminder.Reset(workRecorder.GetTotalNormie8hWorkedToday());
            speech.SpeakOnSoundDevice(settings.SayOnStartup);
            logger.LogDebug("Startup finished");
        }

        /// <summary>
        /// The main loop that continuously monitors user activity and enforces reminders.
        /// </summary>
        private void Loop()
        {
            var detectors = new IActivityDetector[] { inputDetector, callDetector };
            this.LastChecked = DateTime.Now;

            while (!this.disposedValue)
            {
                var detected = detectors.Select(d => d.Detect()).ToList();
                var anyActivityDetected = detected.Any(d => d);
                var timeSinceLastCheck = currentTime.Now - this.LastChecked;
                this.LastChecked = DateTime.Now;
                if (anyActivityDetected && timeSinceLastCheck >= settings.MaxCheckCadence + TimeSpan.FromSeconds(5))
                {
                    // system was probably suspended or hibernated, so let's simulate an idle (no activity) event this time to allow a break to be recorded for this idle time
                    anyActivityDetected = false;
                }

                doNotDisturb.ToggleIfNeeded();
                this.PerformActivityCheck(anyActivityDetected);

                ActivityCheckPerformed?.Invoke(this, EventArgs.Empty);

                this.UpdateThreadWaiter.WaitOne(this.TimeUntilNextCheck);
            }
        }

        /// <summary>
        /// Performs an activity check to determine if a break or reminder is needed.
        /// </summary>
        /// <param name="anyActivityDetected">Indicates if any activity was detected.</param>
        public void PerformActivityCheck(bool anyActivityDetected)
        {
            if (anyActivityDetected)
            {
                if (deliberateActivityFilter.IsDeliberateActivity())
                {
                    this.DeliberateActivityDetected();
                }
                else
                {
                    logger.LogDebug("Activity detected, but not deemed deliberate yet");
                }
            }
            else
            {
                // No activity detected time time, but this doesn't mean much by itself

                if (this.CurrentIdleTime > settings.MinBreak)
                {
                    // Looks like a proper break was taken!

                    this.BreakDetected();
                }
                else
                {
                    logger.LogDebug("No activity detected, but not calling a break yet.");
                }
            }
        }

        /// <summary>
        /// Handles deliberate activity detection, updating reminders and logging activity.
        /// </summary>
        private void DeliberateActivityDetected()
        {
            logger.LogDebug("DeliberateActivityDetected");
            this.LastActive = currentTime.Now;
            workRecorder.LogActivity();
            this.IdleTimeReminder.Reset(TimeSpan.Zero);

            if (this.WorkSessionReminder.IsItTimeToTriggerAnotherReminder(this.ActivityTime, () => !this.SuperBusyRightNow()))
            {
                this.TriggerWorkedTooMuchInSession();
                deliberateActivityFilter.Reset();
            }

            if (this.DayWorkReminder.IsItTimeToTriggerAnotherReminder(workRecorder.GetTotalNormie8hWorkedToday()))
            {
                this.TriggerWorkedTooMuchToday();
            }
        }

        /// <summary>
        /// Handles break detection, resetting reminders and updating the last break timestamp.
        /// </summary>
        private void BreakDetected()
        {
            logger.LogDebug("BreakDetected");
            this.LastBreak = currentTime.Now;
            this.WorkSessionReminder.Reset();
            deliberateActivityFilter.Reset();

            if (this.IdleTimeReminder.IsItTimeToTriggerAnotherReminder(this.CurrentIdleTime))
            {
                this.TriggerBreakTooLong();
            }
        }

        /// <summary>
        /// Sends a voice notification to the user.
        /// </summary>
        /// <param name="text">The text to be spoken. If null, a default message is used.</param>
        public void VoiceNotification(string text = null)
        {
            logger.LogDebug("VoiceNotification({text})", text);
            text ??= settings.VoiceNotificationTextProgression?[this.WorkSessionReminder.ReminderCount - 1] ?? "Time for a break, bud!";
            speech.SpeakOnSoundDevice(text);
        }

        /// <summary>
        /// Triggers a reminder when the user has worked too much in a single session.
        /// </summary>
        private void TriggerWorkedTooMuchInSession()
        {
            logger.LogDebug("TriggerWorkedTooMuchInSession");
            this.IdleTimeReminder.Reset();

            this.VoiceNotification();

            BreakSuggested?.Invoke(this, EventArgs.Empty);

            if (this.WorkSessionReminder.ReminderCount > settings.EscalationAfterSubsequentReminders)
            {
                this.SendComplaintAboutUnbrokenActivityTime();
            }
        }

        /// <summary>
        /// Triggers a reminder when the user has worked too much in a single day.
        /// </summary>
        private void TriggerWorkedTooMuchToday()
        {
            logger.LogDebug("TriggerWorkedTooMuchToday");
            dispatcher.Invoke(() => speech.SpeakOnSoundDevice(string.Format(settings.VoiceNotificationDayWorked, workRecorder.GetTotalNormie8hWorkedToday().Hours)));

            if (this.DayWorkReminder.ReminderCount > settings.EscalationAfterSubsequentRemindersDayWorked)
            {
                this.SendComplaintAboutTotalWorkDayTime(workRecorder.GetTotalNormie8hWorkedToday());
            }
        }

        /// <summary>
        /// Triggers a reminder when the user's break has been too long.
        /// </summary>
        private void TriggerBreakTooLong()
        {
            logger.LogDebug("TriggerBreakTooLong");
            this.VoiceNotification(settings.VoiceNotificationBackToWork);
        }

        /// <summary>
        /// Sends an escalation notification about unbroken activity time.
        /// </summary>
        private void SendComplaintAboutUnbrokenActivityTime()
        {
            logger.LogDebug("SendComplaintAboutUnbrokenActivityTime");
            if (!string.IsNullOrWhiteSpace(settings.EscalationNotificationEmailTo) &&
                    !string.IsNullOrWhiteSpace(settings.EscalationNotificationEmailFrom) &&
                    !string.IsNullOrWhiteSpace(settings.EscalationNotificationEmailFromPass))
            {
                this.WorkSessionReminder.ReminderCount = 0;
                try
                {
                    humanEscalator.SendNotification(settings.EscalationNotificationEmailTo, settings.EscalationNotificationEmailFrom, settings.EscalationNotificationEmailFromPass, settings.EscalationNotificationText);
                }
                catch { }
            }
        }

        /// <summary>
        /// Sends an escalation notification about exceeding the total workday time.
        /// </summary>
        /// <param name="workedToday">The total time worked today.</param>
        private void SendComplaintAboutTotalWorkDayTime(TimeSpan workedToday)
        {
            logger.LogDebug("SendComplaintAboutTotalWorkDayTime");
            if (!string.IsNullOrWhiteSpace(settings.EscalationNotificationEmailTo) &&
                    !string.IsNullOrWhiteSpace(settings.EscalationNotificationEmailFrom) &&
                    !string.IsNullOrWhiteSpace(settings.EscalationNotificationEmailFromPass))
            {
                this.DayWorkReminder.ReminderCount = 0;
                try
                {
                    humanEscalator.SendNotification(settings.EscalationNotificationEmailTo, settings.EscalationNotificationEmailFrom, settings.EscalationNotificationEmailFromPass, string.Format(settings.EscalationNotificationTextDayWorked, workedToday.Hours));
                }
                catch { }
            }
        }

        /// <summary>
        /// Determines if the user is currently busy with a call or similar activity.
        /// </summary>
        /// <returns>True if the user is busy; otherwise, false.</returns>
        private bool SuperBusyRightNow()
        {
            return callDetector.IsActive || callDetector.LastActive >= currentTime.Now - settings.MinCheckCadence;
        }

        /// <summary>
        /// Disposes of the resources used by the MainLoop.
        /// </summary>
        public void Dispose()
        {
            logger.LogDebug("Finishing up");
            this.disposedValue = true;
            workRecorder.Dispose();
            GC.SuppressFinalize(this);
            logger.LogDebug("Finished");
        }
    }
}
