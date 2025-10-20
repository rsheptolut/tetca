using System;

namespace Tetca.Logic
{
    /// <summary>
    /// Represents a trigger for reminders, managing the timing and count of reminders.
    /// </summary>
    /// <param name="currentTime">An instance of <see cref="ICurrentTime"/> to provide the current time.</param>
    /// <param name="getInterval">A function to calculate the interval between reminders based on the reminder count.</param>
    /// <param name="maxReminders">The maximum number of reminders allowed. Null for unlimited reminders.</param>
    /// <param name="isAGoodTime">Optional predicate to check if it's a good time to trigger a reminder.</param>
    internal class ReminderTrigger(ICurrentTime currentTime, Func<int, TimeSpan> getInterval, int? maxReminders = null, Func<DateTime, bool> isAGoodTime = null)
    {
        /// <summary>
        /// Gets or sets the total time already reminded.
        /// </summary>
        public TimeSpan AlreadyReminded { get; set; }

        /// <summary>
        /// Gets the timestamp of the last reminder.
        /// </summary>
        public DateTime LastReminder { get; private set; }

        /// <summary>
        /// Gets the interval for the next reminder based on the current reminder count.
        /// </summary>
        public TimeSpan ReminderInterval => getInterval(this.ReminderCount);

        /// <summary>
        /// Gets or sets the count of reminders triggered so far.
        /// </summary>
        public int ReminderCount { get; set; }

        /// <summary>
        /// Determines whether it is time to trigger another reminder.
        /// </summary>
        /// <param name="totalTime">The total elapsed time.</param>
        /// <param name="doesThisOneCount">An optional function to determine if the current reminder should count.</param>
        /// <returns>True if it is time to trigger another reminder; otherwise, false.</returns>
        public bool IsItTimeToTriggerAnotherReminder(TimeSpan totalTime, Func<bool> doesThisOneCount = null)
        {
            var now = currentTime.Now;
            if (!(this.ReminderCount > maxReminders) && totalTime - this.AlreadyReminded > this.ReminderInterval && isAGoodTime?.Invoke(now) != false)
            {
                this.AlreadyReminded = totalTime;
                this.LastReminder = now;

                if (doesThisOneCount?.Invoke() != false)
                {
                    this.ReminderCount++;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the expected timestamp for the next reminder trigger.
        /// </summary>
        public DateTime NextTriggerExpected => this.LastReminder + this.ReminderInterval;

        /// <summary>
        /// Resets the reminder state with a specific reminder count and already reminded time.
        /// </summary>
        /// <param name="reminderCount">The new reminder count.</param>
        /// <param name="alreadyReminded">The new already reminded time.</param>
        public void Reset(int reminderCount, TimeSpan alreadyReminded)
        {
            this.AlreadyReminded = alreadyReminded;
            this.ReminderCount = reminderCount;
        }

        /// <summary>
        /// Resets the reminder state with a specific already reminded time.
        /// </summary>
        /// <param name="alreadyReminded">The new already reminded time.</param>
        public void Reset(TimeSpan alreadyReminded)
        {
            this.AlreadyReminded = alreadyReminded;
        }

        /// <summary>
        /// Resets the reminder state with a specific reminder count.
        /// </summary>
        /// <param name="reminderCount">The new reminder count.</param>
        public void Reset(int reminderCount)
        {
            this.ReminderCount = reminderCount;
        }

        /// <summary>
        /// Resets the reminder state to its initial values.
        /// </summary>
        public void Reset()
        {
            this.AlreadyReminded = TimeSpan.Zero;
            this.ReminderCount = 0;
        }
    }
}
