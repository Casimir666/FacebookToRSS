using System;
using System.Globalization;

namespace FacebookToRSS
{
    public static class Utilities
    {
        public static DateTime ParseFacebookDate(string facebookDate, CultureInfo cultureInfo, DateTime now)
        {
            if (DateTime.TryParseExact(facebookDate, "d MMMM, HH:mm", cultureInfo, DateTimeStyles.None, out DateTime postDateTime) ||
                DateTime.TryParseExact(facebookDate, "d MMMM", cultureInfo, DateTimeStyles.None, out postDateTime))
            {
                return postDateTime;
            }

            if (TimeSpan.TryParseExact(facebookDate, "%h\\ \\h", cultureInfo, out TimeSpan postDuration))
                return new DateTime (now.Year, now.Month, now.Day, now.Hour, 0, 0) - postDuration;  // Hack : round to lower hour

            if (TimeSpan.TryParseExact(facebookDate, "%m\\ \\m", cultureInfo, out postDuration) ||
                TimeSpan.TryParseExact(facebookDate, "%m\\ \\m\\i\\n", cultureInfo, out postDuration))
            {
                return new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0) - postDuration + TimeSpan.FromMinutes(1); // Hack : round to lower minute
            }

            if (TimeSpan.TryParseExact(facebookDate, "\\H\\i\\e\\r\\,\\ \\à\\ %h\\:%m", cultureInfo, out postDuration))
            {
                return new DateTime(now.Year, now.Month, now.Day, postDuration.Hours, postDuration.Minutes, 0, DateTimeKind.Local) - TimeSpan.FromDays(1);
            }

            throw new FacebookException($"Date format invalid for post: {facebookDate}");
        }
    }
}
