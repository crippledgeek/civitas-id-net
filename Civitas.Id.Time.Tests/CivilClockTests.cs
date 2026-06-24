namespace Civitas.Id.Time.Tests;

public class CivilClockTests
{
    private static readonly TimeZoneIds StockholmIds = new("Europe/Stockholm", "W. Europe Standard Time");

    public class Construction
    {
        [Test]
        public async Task Ctor_NullIds_Throws()
        {
            var ex = await Assert.That(() => new CivilClock(null!)).Throws<ArgumentNullException>();
            await Assert.That(ex!.ParamName).IsEqualTo("ids");
        }
    }

    public class TodayBehavior
    {
        // 2024-07-01T22:30:00Z is 2024-07-02 00:30 in Stockholm (CEST, UTC+2):
        // proves the clock uses the civil zone, not UTC.
        [Test]
        public async Task Today_WithProvider_UsesCivilZoneNotUtc()
        {
            var clock = new CivilClock(StockholmIds);
            var fixedProvider = new FixedTimeProvider(new DateTimeOffset(2024, 7, 1, 22, 30, 0, TimeSpan.Zero));

            var today = clock.Today(fixedProvider);

            await Assert.That(today).IsEqualTo(new DateOnly(2024, 7, 2));
        }

        [Test]
        public async Task Today_NullProvider_Throws()
        {
            var clock = new CivilClock(StockholmIds);
            var ex = await Assert.That(() => clock.Today(null!)).Throws<ArgumentNullException>();
            await Assert.That(ex!.ParamName).IsEqualTo("timeProvider");
        }

        [Test]
        public async Task TimeZone_ResolvesToIanaZone()
        {
            var clock = new CivilClock(StockholmIds);
            await Assert.That(clock.TimeZone.Id).IsEqualTo("Europe/Stockholm");
        }
    }

    public class ZoneResolution
    {
        private static readonly TimeZoneInfo Sentinel =
            TimeZoneInfo.CreateCustomTimeZone("Sentinel/Zone", TimeSpan.FromHours(5), "Sentinel", "Sentinel");

        [Test]
        public async Task IanaFound_ReturnsProbe1_WithoutProbingWindows()
        {
            var calls = new List<string>();
            var result = CivilClock.Resolve(StockholmIds,
                id => { calls.Add(id); return id == "Europe/Stockholm" ? Sentinel : null; });

            await Assert.That(result).IsEqualTo(Sentinel);
            await Assert.That(calls.Count).IsEqualTo(1);
            await Assert.That(calls[0]).IsEqualTo("Europe/Stockholm");
        }

        [Test]
        public async Task IanaMissing_FallsBackToWindows()
        {
            var result = CivilClock.Resolve(StockholmIds,
                id => id == "W. Europe Standard Time" ? Sentinel : null);

            await Assert.That(result).IsEqualTo(Sentinel);
        }

        [Test]
        public async Task NeitherFound_ThrowsWithDeploymentHint()
        {
            var ex = await Assert.That(() => CivilClock.Resolve(new TimeZoneIds("X/Y", "Z"), _ => null))
                .Throws<InvalidOperationException>();
            await Assert.That(ex!.Message).Contains("tzdata");
        }

        [Test]
        public async Task ThrownMessage_EchoesBothIdentifiers()
        {
            var ex = await Assert.That(() => CivilClock.Resolve(new TimeZoneIds("X/Y", "Z"), _ => null))
                .Throws<InvalidOperationException>();
            await Assert.That(ex!.Message).Contains("'X/Y'");
            await Assert.That(ex.Message).Contains("'Z'");
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
