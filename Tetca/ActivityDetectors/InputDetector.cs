using Tetca.Logic;
using System;
using System.Runtime.InteropServices;

namespace Tetca.ActivityDetectors
{
    /// <summary>
    /// Detects user input activity (keyboard or mouse) by monitoring the system's last input time.
    /// </summary>
    public class InputDetector(ICurrentTime currentTime) : IActivityDetector
    {
        private readonly ICurrentTime currentTime = currentTime;

        private TimeSpan? inputMs = null;
        private TimeSpan? prevInputMs = null;

        /// <summary>
        /// Gets or sets the last time user input was detected. This is updated whenever new input is detected.
        /// </summary>
        public DateTime LastActive { get; set; } = currentTime.Now;

        /// <summary>
        /// Gets or sets a value indicating whether user input is currently detected.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets the description of the last detected activity. This is used for logging and debugging purposes.
        /// </summary>
        public string LastActivityDescription => "User input";

        /// <summary>
        /// Detects if user input (keyboard or mouse) has occurred since the last check.
        /// Updates the <see cref="LastActive"/> property if new input is detected.
        /// </summary>
        /// <returns>True if new input is detected; otherwise, false.</returns>
        public bool Detect()
        {
            var newInputDate = this.GetNewInputSinceLastCheck();
            this.LastActive = newInputDate ?? this.LastActive;
            this.IsActive = newInputDate != null;
            return this.IsActive;
        }

        /// <summary>
        /// Gets the time of the new input since the last check.
        /// </summary>
        /// <returns>
        /// Returns the time of the new input. If no new input was detected since the last check, null is returned.
        /// </returns>
        private DateTime? GetNewInputSinceLastCheck()
        {
            var inputMs = GetLastInputTicks();
            if (inputMs != null)
            {
                this.prevInputMs = this.inputMs;
                this.inputMs = inputMs;
                if (this.inputMs != this.prevInputMs && this.prevInputMs != null)
                {
                    return RoundToSeconds(this.currentTime.Now - new TimeSpan(Environment.TickCount - this.inputMs.Value.Ticks));
                }
            }

            return null;
        }

        /// <summary>
        /// Populates <see cref="LASTINPUTINFO.Time"/> with the number of milliseconds since system startup
        /// when the last keyboard or mouse input was detected.
        /// </summary>
        /// <param name="lastInputInfo">The structure to be populated with the last input information.</param>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        [DllImport("User32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO lastInputInfo);

        /// <summary>
        /// Returns the time elapsed since system startup when the last keyboard or mouse input was detected.
        /// </summary>
        /// <returns>
        /// A <see cref="TimeSpan"/> representing the time elapsed since system startup when the last input was detected,
        /// or null if the operation was unsuccessful.
        /// </returns>
        private static TimeSpan? GetLastInputTicks()
        {
            LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
            lastInputInfo.Size = (uint)Marshal.SizeOf(lastInputInfo);
            return GetLastInputInfo(ref lastInputInfo) ? new TimeSpan(lastInputInfo.Time) : null;
        }

        /// <summary>
        /// Represents the structure to be filled out by <see cref="GetLastInputInfo"/>.
        /// </summary>
        internal struct LASTINPUTINFO
        {
            /// <summary>
            /// The size of the structure, in bytes.
            /// </summary>
            public uint Size;

            /// <summary>
            /// The tick count at the time of the last input event.
            /// </summary>
            public uint Time;
        }

        /// <summary>
        /// Rounds the given <see cref="DateTime"/> to the nearest second.
        /// </summary>
        /// <param name="t">The <see cref="DateTime"/> to round.</param>
        /// <returns>A new <see cref="DateTime"/> rounded to the nearest second.</returns>
        private static DateTime RoundToSeconds(DateTime t) => new DateTime(t.Ticks / TimeSpan.FromSeconds(1).Ticks * TimeSpan.FromSeconds(1).Ticks);
    }
}
