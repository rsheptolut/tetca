using System;
using Tetca.ActivityDetectors;
using Tetca.Logic;

namespace Tetca.Windows.MainWindow
{
    /// <summary>
    /// Represents the debug state of the application, providing information about user activity and call detection.
    /// </summary>
    public class DebugState(InputDetector inputDetector, CallDetector callDetector, MainLoop mainLoop)
    {
        /// <summary>
        /// Gets the time worked in this session
        /// </summary>
        public TimeSpan ActivityTime => mainLoop.ActivityTime;

        /// <summary>
        /// Gets the time worked today for a normie 8-hour workday.
        /// </summary>
        public TimeSpan WorkedTodayNormie8h => mainLoop.HoursWorkedToday;

        /// <summary>
        /// Gets the time worked today for a custom workday.
        /// </summary>
        public DateTime LastActivity => mainLoop.LastActive;

        /// <summary>
        /// Gets the time since the last activity was detected.
        /// </summary>
        public TimeSpan CurrentIdleTime => mainLoop.CurrentIdleTime;

        /// <summary>
        /// Gets the time since the last break was detected.
        /// </summary>
        public bool CallDetected => callDetector.IsActive;

        /// <summary>
        /// Gets the time since the last input was detected.
        /// </summary>
        public bool InputDetected => inputDetector.IsActive;
    }
}
