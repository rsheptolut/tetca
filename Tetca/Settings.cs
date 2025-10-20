using System;

namespace Tetca
{
    public class Settings
    {
        /// <summary>
        /// A phrase to send to the speech synthesizer upon startup
        /// </summary>
        public string SayOnStartup { get; set; }

        /// <summary>
        /// Program will try not to check your activity faster than this
        /// </summary>
        public TimeSpan MinCheckCadence { get; set; }

        /// <summary>
        /// Program will aim to check your activity at least as fast as this
        /// </summary>
        public TimeSpan MaxCheckCadence { get; set; }

        /// <summary>
        /// How long you want your break to be at the very minimum. Inactivity shorter than this break will count towards Activity
        /// </summary>
        public TimeSpan MinBreak { get; set; }

        /// <summary>
        /// How long you want your break to be before you're reminded to get back to work
        /// </summary>
        public TimeSpan MaxBreak { get; set; }

        /// <summary>
        /// Text that will be spoken when the program reminding you to get back to work
        /// </summary>
        public string VoiceNotificationBackToWork { get; set; }

        /// <summary>
        /// How long you want to work before you're reminded to take a break
        /// </summary>
        public TimeSpan MaxWorkSession { get; set; }

        /// <summary>
        /// The name of your default sound device that is physically turned ON at all times to hear your notifications always. The program will switch to it every time it speaks.
        /// </summary>
        public string VoiceNotificationSoundDevice { get; set; }

        /// <summary>
        /// The language/culture of the voice that you want to hear, defaults to en-US
        /// </summary>
        public string VoiceNotificationCulture { get; set; }

        /// <summary>
        /// The text that will be spoken when the program reminding you to take a break. If multiple are specified, they're rotated based on how many notifications you've already dismissed for that work session.
        /// </summary>
        public string[] VoiceNotificationTextProgression { get; set; }

        /// <summary>
        /// Time to wait before second, third, etc reminders to take a break
        /// </summary>
        public TimeSpan SubsequentReminderInterval { get; set; }

        /// <summary>
        /// How many reminders to take a break you want to receive before the program sends a message to your human buddy who can talk some sense into you
        /// </summary>
        public int? EscalationAfterSubsequentReminders { get; set; }

        /// <summary>
        /// Your email address (create a dedicated email account for this, for security)
        /// </summary>
        public string EscalationNotificationEmailFrom { get; set; }

        /// <summary>
        /// The password to your email address (create a dedicated email account for this, for security)
        /// </summary>
        public string EscalationNotificationEmailFromPass { get; set; }

        /// <summary>
        /// Email address of your human buddy (or multiple, comma separated)
        /// </summary>
        public string EscalationNotificationEmailTo { get; set; }

        /// <summary>
        /// Text that will be sent to your human buddy when the program is trying to get you to take a break.
        /// </summary>
        public string EscalationNotificationText { get; set; }
        /// <summary>
        /// How long you want to work in a work day before you're reminded to stop working for today
        /// </summary>
        public TimeSpan MaxDayWorkTime { get; set; }

        /// <summary>
        /// How long you want to be able to work before you're again reminded to stop working for today
        /// </summary>
        public TimeSpan DayWorkTimeNotificationInterval { get; set; }

        /// <summary>
        /// Text that will be spoken when the program reminding you to stop working for today
        /// </summary>
        public string VoiceNotificationDayWorked { get; set; }

        /// <summary>
        /// How many time you want to be reminded to stop working for the day before this matter is escalated to your human buddy.
        /// </summary>
        public int? EscalationAfterSubsequentRemindersDayWorked { get; set; }

        /// <summary>
        /// Text that will be sent to your human buddy when the program determines you've worked too much for today.
        /// </summary>
        public string EscalationNotificationTextDayWorked { get; set; }

        /// <summary>
        /// Time of day when you're most likely to already be in bed yet very unlikely to already be up. This helps to shift your workday into the later hours instead of treating the midnight as the end of your workday.
        /// </summary>
        public TimeSpan LatestGoToBedTimeForReporting { get; set; }

        /// <summary>
        /// Start of work hours (will unmute the speaker)
        /// </summary>
        public TimeSpan WorkHoursFrom { get; set; }

        /// <summary>
        /// End of work hours (will mute the speaker)
        /// </summary>
        public TimeSpan WorkHoursTo { get; set; }
    }
}
