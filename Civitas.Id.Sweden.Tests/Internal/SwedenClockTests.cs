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

        [Test]
        public async Task Today_TimeProvider_ReturnsExpectedDate()
        {
            // A fixed UTC instant that is midnight Stockholm time (UTC+2 in summer):
            // 2026-07-15 22:00:00 UTC  →  2026-07-16 00:00:00 CEST.
            var fixedUtc = new DateTimeOffset(2026, 7, 15, 22, 0, 0, TimeSpan.Zero);
            var provider = new FixedTimeProvider(fixedUtc);
            var result = SwedenClock.Today(provider);
            await Assert.That(result).IsEqualTo(new DateOnly(2026, 7, 16));
        }

        [Test]
        public async Task Today_NullTimeProvider_ThrowsArgumentNullException()
        {
            var ex = await Assert.That(() => SwedenClock.Today(null!)).Throws<ArgumentNullException>();
            await Assert.That(ex).IsNotNull();
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

    /// <summary>
    /// Tests for the injectable <c>ResolveStockholmTimeZone(Func&lt;string, TimeZoneInfo?&gt;)</c>
    /// seam. Each test controls exactly which probes succeed or fail, making the
    /// Probe-2 and throw branches test-reachable without modifying the host's tzdata.
    /// </summary>
    public class ResolveStockholmTimeZoneSeam
    {
        private static readonly TimeZoneInfo SentinelZone =
            TimeZoneInfo.CreateCustomTimeZone("Sentinel/Zone", TimeSpan.FromHours(5), "Sentinel", "Sentinel");

        [Test]
        public async Task Probe1_FindReturnsNonNull_ReturnsImmediately()
        {
            // Probe 1: find("Europe/Stockholm") succeeds → sentinel returned.
            var result = SwedenClock.ResolveStockholmTimeZone(
                id => id == "Europe/Stockholm" ? SentinelZone : null);
            await Assert.That(result).IsEqualTo(SentinelZone);
        }

        [Test]
        public async Task Probe2_Probe1NullProbe2NonNull_ReturnsProbe2Result()
        {
            // Probe 2: find("Europe/Stockholm") → null; find("W. Europe Standard Time") → sentinel.
            // This branch was previously unreachable without a modified tzdata environment.
            var result = SwedenClock.ResolveStockholmTimeZone(
                id => id == "W. Europe Standard Time" ? SentinelZone : null);
            await Assert.That(result).IsEqualTo(SentinelZone);
        }

        [Test]
        public async Task BothProbesNull_ThrowsInvalidOperationException_WithDeploymentGuidance()
        {
            // Both probes return null → InvalidOperationException.
            // Assert the message still contains "tzdata" so a future reword cannot
            // silently drop the deployment fix guidance.
            var ex = await Assert.That(() =>
                SwedenClock.ResolveStockholmTimeZone(_ => null)).Throws<InvalidOperationException>();
            await Assert.That(ex!.Message).Contains("tzdata");
        }

        [Test]
        public async Task NullFinder_ThrowsArgumentNullException()
        {
            var ex = await Assert.That(() =>
                SwedenClock.ResolveStockholmTimeZone(null!)).Throws<ArgumentNullException>();
            await Assert.That(ex).IsNotNull();
        }
    }

    /// <summary>
    /// Tests for <c>DefaultFind</c> — the production <see cref="TimeZoneInfo"/> lookup
    /// wrapper that converts "not found" from a thrown exception to <see langword="null"/>.
    /// </summary>
    public class DefaultFindMethod
    {
        [Test]
        public async Task DefaultFind_KnownIdentifier_ReturnsNonNull()
        {
            // "Europe/Stockholm" is present on the test runner host (Linux with tzdata).
            var result = SwedenClock.DefaultFind("Europe/Stockholm");
            await Assert.That(result).IsNotNull();
        }

        [Test]
        public async Task DefaultFind_UnknownIdentifier_ReturnsNull()
        {
            // An identifier that cannot exist in any real tzdata/Windows registry.
            var result = SwedenClock.DefaultFind("Definitely/NotAZone");
            await Assert.That(result).IsNull();
        }
    }

    /// <summary>Minimal deterministic <see cref="TimeProvider"/> for testing.</summary>
    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
