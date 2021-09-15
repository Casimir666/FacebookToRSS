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
        private readonly DateTime _now = new DateTime(2021, 9, 15, 9, 47, 15, DateTimeKind.Local);

        [TestCase("13 min",               "2021/9/15 09:35:15")]
        [TestCase("13 m",                 "2021/9/15 09:35:15")]
        [TestCase("16 h",                 "2021/9/14 18:47:15")]
        [TestCase("Hier, à 15:28",        "2021/9/14 15:28")]
        [TestCase("13 septembre, 09:10",  "2021/9/13 09:10")]
        [TestCase("6 août",               "2021/8/6")]
        public void TestDate(string facebookDate, string expectedDatetime)
        {
            var date = Utilities.ParseFacebookDate(facebookDate, _cultureInfo, _now);

            date.Should().Be(DateTime.Parse(expectedDatetime));
        }
    }
}
