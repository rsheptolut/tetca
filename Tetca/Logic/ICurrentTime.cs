using System;

namespace Tetca.Logic
{
    /// <summary>
    /// An interface to provide the current time and UTC time.
    /// </summary>
    public interface ICurrentTime
    {
        /// <summary>
        /// Gets the current local time.
        /// </summary>
        DateTime Now { get; }

        /// <summary>
        /// Gets the current UTC time.
        /// </summary>
        DateTime UtcNow { get; }
    }
}