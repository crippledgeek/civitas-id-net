using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Internal;

namespace Civitas.Id.Sweden.Tests.Internal;

/// <summary>
///     Tests for <see cref="SwedishIdParsing.TryValidatePersonShapedBody"/> — the
///     shared leaf atomic that performs DaysInMonth + Luhn validation for both the
///     person-ID dispatcher and OrganisationId's Enskild firma branch.
/// </summary>
public class TryValidatePersonShapedBodyTests
{
    // Luhn-valid personnummer: 1989-01-01 with check digit 1 (verified: body
    // 8901010101 → weighted sum 19 → check (10-9)%10 = 1).
    private const string ValidLong12 = "198901010101";

    // SwedishIdMatcher is a nested type on SwedishOfficialId (Civitas.Id.Sweden.Core),
    // NOT on SwedishIdParsing. The TryMatch accessor returns the Core-namespace type.
    private static SwedishOfficialId.SwedishIdMatcher MatchOrThrow(string input)
        => SwedishIdParsing.TryMatch(input)
           ?? throw new InvalidOperationException($"matcher returned null for '{input}'");

    [Test]
    public async Task ValidInput_ReturnsTrue()
    {
        var matcher = MatchOrThrow(ValidLong12);
        var result = SwedishIdParsing.TryValidatePersonShapedBody(matcher, 1989, 1);
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task DayBeyondMonthLength_ReturnsFalse()
    {
        // Feb 30 1989 — calendar-invalid. Date check fires before Luhn so any
        // structurally-valid input string suffices.
        var matcher = MatchOrThrow("198902300107");
        var result = SwedishIdParsing.TryValidatePersonShapedBody(matcher, 1989, 30);
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task LeapDayInNonLeapYear_ReturnsFalse()
    {
        // Feb 29 1991 — not a leap year. Date check fires before Luhn.
        var matcher = MatchOrThrow("199102290107");
        var result = SwedishIdParsing.TryValidatePersonShapedBody(matcher, 1991, 29);
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task LeapDayInLeapYear_ReturnsTrueIfLuhnValid()
    {
        // Feb 29 2000 — leap year. Luhn-valid (body 0002296127 → check 7).
        var matcher = MatchOrThrow("200002296127");
        var result = SwedishIdParsing.TryValidatePersonShapedBody(matcher, 2000, 29);
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task InvalidLuhn_ReturnsFalse()
    {
        // ValidLong12 with check digit changed from 1 to 2 — Luhn-invalid.
        var matcher = MatchOrThrow("198901010102");
        var result = SwedishIdParsing.TryValidatePersonShapedBody(matcher, 1989, 1);
        await Assert.That(result).IsFalse();
    }
}
