using Civitas.Id.Sweden.Internal;

namespace Civitas.Id.Sweden.Tests.Internal;

public class SwedenClockTests
{
    public class TimeZoneProperty
    {
        [Test]
        public async Task TimeZone_Returns_EuropeStockholm()
        {
            await Assert.That(SwedenClock.TimeZone.Id).IsEqualTo("Europe/Stockholm");
        }

        [Test]
        public async Task TimeZone_HasDaylightSavingTime()
        {
            await Assert.That(SwedenClock.TimeZone.SupportsDaylightSavingTime).IsTrue();
        }
    }

    public class Today
    {
        [Test]
        public async Task Today_ReturnsDateOnly_NotThrowing()
        {
            // Smoke — proves the call resolves the IANA id and produces a value.
            var today = SwedenClock.Today();
            await Assert.That(today.Year).IsGreaterThanOrEqualTo(2026);
        }
    }

    public class TimeZoneResolution
    {
        [Test]
        public async Task TimeZone_Resolves_OnNormalHost()
        {
            // Smoke — proves the lazy resolves successfully on the test runner host.
            await Assert.That(SwedenClock.TimeZone).IsNotNull();
        }

        [Test]
        public async Task TimeZone_Id_IsEither_EuropeStockholm_Or_WEuropeStandardTime()
        {
            // Probe 1 (IANA) succeeds on Linux/macOS and on Windows with ICU.
            // Probe 2 (Windows) succeeds on Windows with NLS or non-ICU configs.
            // The library does not care which probe wins — both refer to the
            // same civil offset/DST rules.
            var id = SwedenClock.TimeZone.Id;
            var ok = id is "Europe/Stockholm" or "W. Europe Standard Time";
            await Assert.That(ok).IsTrue();
        }

        [Test]
        public async Task TimeZone_Observes_DST_Transition_To_CEST()
        {
            // Last Sunday of March, summer offset = +02:00 by July
            var summerInstant = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Unspecified);
            var offset = SwedenClock.TimeZone.GetUtcOffset(summerInstant);
            await Assert.That(offset).IsEqualTo(TimeSpan.FromHours(2));
        }

        [Test]
        public async Task TimeZone_Observes_DST_Transition_To_CET()
        {
            // Last Sunday of October through last Sunday of March, winter offset = +01:00
            var winterInstant = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Unspecified);
            var offset = SwedenClock.TimeZone.GetUtcOffset(winterInstant);
            await Assert.That(offset).IsEqualTo(TimeSpan.FromHours(1));
        }
    }
}
