using System;

namespace Tetca.ActivityDetectors
{
    /// <summary>
    /// Detects user activity in one way or another
    /// </summary>
    public interface IActivityDetector
    {
        /// <summary>
        /// Gets set each time to DateTime.Now when activity is detected, so it reflects the last time when it was.
        /// </summary>
        DateTime LastActive { get; }

        /// <summary>
        /// True if activity was detected during the recent call of 'Detect'. Otherwise, false.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Gets the description of the last activity detected. This is used for logging and debugging purposes.
        /// </summary>
        string LastActivityDescription { get; }

        /// <summary>
        /// Main method that needs to be called periodially to check for activity
        /// </summary>
        /// <returns>True if activity is detected. Otherwise, false.</returns>
        bool Detect();
    }
}