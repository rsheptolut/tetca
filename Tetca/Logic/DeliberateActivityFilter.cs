using System;

namespace Tetca.Logic
{
    /// <summary>
    /// A filter to detect deliberate activity based on a series of minor activity events.
    /// </summary>
    public class DeliberateActivityFilter
    {
        private readonly TimeSpan timeout;
        private readonly ICurrentTime currentTime;
        private int minorActivityCount;
        private readonly int minimumMinorActivityEventsToConsiderActive;
        private readonly TimeSpan activityMonitoringInterval;
        private DateTime minorActivityStarted;
        private bool deliberateActivityDetected;
        private DateTime lastActivity;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliberateActivityFilter"/> class.
        /// </summary>
        /// <param name="minCheckCadence">Mininmum cadence of checks</param>
        /// <param name="currentTime">Current time</param>
        public DeliberateActivityFilter(TimeSpan minCheckCadence, TimeSpan timeout, ICurrentTime currentTime)
        {
            this.minorActivityCount = 0;
            this.minimumMinorActivityEventsToConsiderActive = 3;

            this.activityMonitoringInterval = minCheckCadence * 6;
            if (this.activityMonitoringInterval > TimeSpan.FromMinutes(1))
            {
                this.activityMonitoringInterval = TimeSpan.FromMinutes(1);
            }

            this.minorActivityStarted = currentTime.Now.Date.AddDays(-1);
            this.timeout = timeout;
            this.currentTime = currentTime;
        }

        /// <summary>
        /// Checks if the current activity is deliberate.
        /// </summary>
        /// <returns>
        /// True if the activity is considered deliberate; otherwise, false.
        /// </returns>
        public bool IsDeliberateActivity()
        {
            if (this.deliberateActivityDetected)
            {
                if (this.currentTime.Now - this.lastActivity > this.timeout)
                {
                    this.Reset();
                }
                else
                {
                    this.lastActivity = this.currentTime.Now;
                    return true;
                }
            }

            if (this.currentTime.Now - this.minorActivityStarted > this.activityMonitoringInterval)
            {
                // We're outside of the activity monitoring interval, so start counting from scratch
                this.minorActivityCount = 0;
                this.minorActivityStarted = this.currentTime.Now;
            }

            this.minorActivityCount++;

            this.deliberateActivityDetected = this.minorActivityCount >= this.minimumMinorActivityEventsToConsiderActive;

            this.lastActivity = this.currentTime.Now;

            return this.deliberateActivityDetected;
        }

        /// <summary>
        /// Resets the filter to its initial state.
        /// </summary>
        public void Reset()
        {
            this.deliberateActivityDetected = false;
            this.minorActivityCount = 0;
            this.minorActivityStarted = this.currentTime.Now.Date.AddDays(-1);
        }
    }
}
