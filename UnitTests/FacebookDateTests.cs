using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using FacebookToRSS;
using FluentAssertions;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class FacebookDateTests
    {
        private readonly CultureInfo _cultureInfo = new CultureInfo("fr-FR");

        [TestCase("13 min",               "2021/9/15 09:23:00", "2021/9/15 09:35:15")]
        [TestCase("25 m",                 "2021/9/15 09:23:00", "2021/9/15 09:47:59")]
        [TestCase("1 h",                  "2021/9/15 08:00:00", "2021/9/15 09:47:15")]
        [TestCase("2 h",                  "2021/9/15 08:00:00", "2021/9/15 10:01:15")]
        [TestCase("17 h",                 "2021/9/15 08:00:00", "2021/9/16 01:00:01")]
        [TestCase("Hier, à 15:28",        "2021/9/14 15:28")]
        [TestCase("13 septembre, 09:10",  "2021/9/13 09:10")]
        [TestCase("6 août",               "2021/8/6")]
        public void TestDate(string facebookDate, string expectedDatetime, string nowLocal = null)
        {
            var now = DateTime.Parse(nowLocal ?? "2021/09/15 09:47:15", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
            var date = Utilities.ParseFacebookDate(facebookDate, _cultureInfo, now);

            date.Should().Be(DateTime.Parse(expectedDatetime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal));
        }
    }
}
