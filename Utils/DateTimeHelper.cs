namespace Utils
{
    public static class DateTimeHelper
    {
        private static readonly TimeZoneInfo VietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

        /// <summary>
        /// Get current time in Vietnam timezone (UTC+7)
        /// </summary>
        public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone);

        /// <summary>
        /// Convert UTC time to Vietnam time
        /// </summary>
        public static DateTime ToVietnamTime(DateTime utcTime)
        {
            if (utcTime.Kind != DateTimeKind.Utc)
            {
                utcTime = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);
            }
            return TimeZoneInfo.ConvertTimeFromUtc(utcTime, VietnamTimeZone);
        }

        /// <summary>
        /// Convert Vietnam time to UTC
        /// </summary>
        public static DateTime ToUtc(DateTime vietnamTime)
        {
            return TimeZoneInfo.ConvertTimeToUtc(vietnamTime, VietnamTimeZone);
        }
    }
}
