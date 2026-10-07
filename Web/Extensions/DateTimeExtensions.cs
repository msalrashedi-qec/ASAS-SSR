namespace Web.Extensions
{
    public static class DateTimeExtensions
    {
        public static DateTime GetKsaDateTime(this DateTime dateTime)
        {
            var saudiTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Arab Standard Time");
            var saudiDateTime = TimeZoneInfo.ConvertTimeFromUtc(dateTime, saudiTimeZone);

            return saudiDateTime;
        }
    }
}
