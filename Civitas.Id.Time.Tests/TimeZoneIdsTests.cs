namespace Civitas.Id.Time.Tests;

public class TimeZoneIdsTests
{
    public class Construction
    {
        [Test]
        [Arguments(null, "W. Europe Standard Time")]
        [Arguments("", "W. Europe Standard Time")]
        [Arguments("Europe/Stockholm", null)]
        [Arguments("Europe/Stockholm", "")]
        public async Task Ctor_RejectsNullOrEmpty(string? iana, string? windows)
        {
            await Assert.That(() => new TimeZoneIds(iana!, windows!)).Throws<ArgumentException>();
        }

        [Test]
        public async Task Ctor_ValidIds_ExposesComponents()
        {
            var ids = new TimeZoneIds("Europe/Stockholm", "W. Europe Standard Time");
            await Assert.That(ids.Iana).IsEqualTo("Europe/Stockholm");
            await Assert.That(ids.Windows).IsEqualTo("W. Europe Standard Time");
        }
    }

    public class Equality
    {
        [Test]
        public async Task SameComponents_AreEqual()
        {
            var a = new TimeZoneIds("Europe/Stockholm", "W. Europe Standard Time");
            var b = new TimeZoneIds("Europe/Stockholm", "W. Europe Standard Time");
            await Assert.That(a).IsEqualTo(b);
        }

        [Test]
        public async Task DifferentIana_AreNotEqual()
        {
            var a = new TimeZoneIds("Europe/Stockholm", "W. Europe Standard Time");
            var b = new TimeZoneIds("Europe/Berlin", "W. Europe Standard Time");
            await Assert.That(a).IsNotEqualTo(b);
        }

        [Test]
        public async Task DifferentWindows_AreNotEqual()
        {
            var a = new TimeZoneIds("Europe/Stockholm", "W. Europe Standard Time");
            var b = new TimeZoneIds("Europe/Stockholm", "Central Europe Standard Time");
            await Assert.That(a).IsNotEqualTo(b);
        }
    }
}
