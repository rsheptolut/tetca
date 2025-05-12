using System;

namespace Tetca.Logic
{
    /// <summary>
    /// Uses <see cref="DateTime"/> to provide the current time and UTC time.
    /// </summary>
    public class CurrentTime : ICurrentTime
    {
        /// <inheritdoc />
        public DateTime Now => DateTime.Now;

        /// <inheritdoc />
        public DateTime UtcNow => DateTime.UtcNow;
    }
}