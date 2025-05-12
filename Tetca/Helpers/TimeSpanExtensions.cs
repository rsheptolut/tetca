using System;
using System.Text;

namespace Tetca.Helpers
{
    /// <summary>
    /// Provides extension methods for the <see cref="TimeSpan"/> structure to perform common operations and formatting.
    /// </summary>
    public static class TimeSpanExtensions
    {
        /// <summary>
        /// Returns the smaller of two <see cref="TimeSpan"/> values.
        /// </summary>
        /// <param name="self">The first <see cref="TimeSpan"/> value.</param>
        /// <param name="other">The second <see cref="TimeSpan"/> value.</param>
        /// <returns>The smaller of the two <see cref="TimeSpan"/> values.</returns>
        public static TimeSpan Min(this TimeSpan self, TimeSpan other) => !(self < other) ? other : self;

        /// <summary>
        /// Returns the larger of two <see cref="TimeSpan"/> values.
        /// </summary>
        /// <param name="self">The first <see cref="TimeSpan"/> value.</param>
        /// <param name="other">The second <see cref="TimeSpan"/> value.</param>
        /// <returns>The larger of the two <see cref="TimeSpan"/> values.</returns>
        public static TimeSpan Max(this TimeSpan self, TimeSpan other) => !(self > other) ? other : self;

        /// <summary>
        /// Converts the <see cref="TimeSpan"/> to a short time string in the format "HH:mm:ss".
        /// </summary>
        /// <param name="self">The <see cref="TimeSpan"/> to format.</param>
        /// <returns>A string representation of the <see cref="TimeSpan"/> in "HH:mm:ss" format.</returns>
        public static string ToShortTimeString(this TimeSpan self) => string.Format("{0}:{1}:{2}", ((int)self.TotalHours).ToString().PadLeft(2, '0'), self.Minutes.ToString().PadLeft(2, '0'), self.Seconds.ToString().PadLeft(2, '0'));

        /// <summary>
        /// Converts the <see cref="TimeSpan"/> to a string representation in hours, minutes, and seconds (e.g., "1h 30m 45s").
        /// </summary>
        /// <param name="self">The <see cref="TimeSpan"/> to format.</param>
        /// <returns>A string representation of the <see cref="TimeSpan"/> in hours, minutes, and seconds.</returns>
        public static string ToHoursAndMinutes(this TimeSpan self)
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (self.Hours > 0)
                stringBuilder.AppendFormat("{0}h", self.Hours);
            if (self.Hours > 0 && self.Minutes > 0)
                stringBuilder.Append(' ');
            if (self.Hours == 0 || self.Minutes != 0)
                stringBuilder.AppendFormat("{0}m", self.Minutes);
            if (self.Hours > 0 || self.Minutes > 0)
                stringBuilder.Append(' ');
            if (self.Hours == 0 || self.Minutes == 0 || self.Seconds > 0)
                stringBuilder.AppendFormat("{0}s", self.Seconds);
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Converts the <see cref="TimeSpan"/> to a long-form string representation in hours, minutes, and seconds (e.g., "1 hour and 30 minutes and 45 seconds").
        /// </summary>
        /// <param name="self">The <see cref="TimeSpan"/> to format.</param>
        /// <returns>A long-form string representation of the <see cref="TimeSpan"/> in hours, minutes, and seconds.</returns>
        public static string ToHoursAndMinutesLong(this TimeSpan self)
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (self.Hours > 0)
                stringBuilder.AppendFormat(Pluralize(self.Hours, "{0} hour", "{0} hours"), self.Hours);
            if (self.Hours > 0 && self.Minutes > 0)
                stringBuilder.Append(" and");
            if (self.Hours == 0 || self.Minutes != 0)
                stringBuilder.AppendFormat(Pluralize(self.Minutes, " {0} minute", " {0} minutes"), self.Minutes);
            if (self.Hours > 0 || self.Minutes > 0)
                stringBuilder.Append(" and");
            if (self.Hours == 0 || self.Minutes == 0 || self.Seconds > 0)
                stringBuilder.AppendFormat(Pluralize(self.Seconds, " {0} second", " {0} seconds"), self.Seconds);
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Returns the singular or plural form of a string based on the specified count.
        /// </summary>
        /// <param name="n">The count to evaluate.</param>
        /// <param name="singular">The singular form of the string.</param>
        /// <param name="plural">The plural form of the string.</param>
        /// <returns>The singular or plural form of the string based on the count.</returns>
        private static string Pluralize(int n, string singular, string plural) => n != 1 ? plural : singular;
    }
}
