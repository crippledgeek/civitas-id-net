using Civitas.Id.Sweden.Internal;

namespace Civitas.Id.Sweden.Tests.Internal;

/// <summary>
///     Tests for <c>SwedishIdParsing.TryValidatePersonShapedBody</c> — the
///     shared leaf atomic that performs DaysInMonth + Luhn validation for both the
///     person-ID dispatcher and OrganisationId's Enskild firma branch.
/// </summary>
public class TryValidatePersonShapedBodyTests
{
    // Luhn-valid personnummer: 1989-01-01 with check digit 1 (verified: body
    // 8901010101 → weighted sum 19 → check (10-9)%10 = 1).
    private const string ValidLong12 = "198901010101";

    /// <summary>
    ///     Runs the structural parse + the leaf validation entirely synchronously
    ///     and returns the two booleans. The match itself is a <c>ref struct</c>
    ///     that cannot survive an <see langword="await"/> (CS4007), so the test
    ///     methods only ever assert on the captured primitives.
    /// </summary>
    private static (bool Matched, bool Valid) Run(string input, int fullYear, int realDay)
    {
        if (!SwedishIdParsing.TryMatchSpan(input.AsSpan(), out var match))
            return (false, false);
        var valid = SwedishIdParsing.TryValidatePersonShapedBody(in match, fullYear, realDay);
        return (true, valid);
    }

    [Test]
    public async Task ValidInput_ReturnsTrue()
    {
        var (matched, valid) = Run(ValidLong12, 1989, 1);
        await Assert.That(matched).IsTrue();
        await Assert.That(valid).IsTrue();
    }

    [Test]
    public async Task DayBeyondMonthLength_ReturnsFalse()
    {
        // Feb 30 1989 — calendar-invalid. Date check fires before Luhn so any
        // structurally-valid input string suffices.
        var (matched, valid) = Run("198902300107", 1989, 30);
        await Assert.That(matched).IsTrue();
        await Assert.That(valid).IsFalse();
    }

    [Test]
    public async Task LeapDayInNonLeapYear_ReturnsFalse()
    {
        // Feb 29 1991 — not a leap year. Date check fires before Luhn.
        var (matched, valid) = Run("199102290107", 1991, 29);
        await Assert.That(matched).IsTrue();
        await Assert.That(valid).IsFalse();
    }

    [Test]
    public async Task LeapDayInLeapYear_ReturnsTrueIfLuhnValid()
    {
        // Feb 29 2000 — leap year. Luhn-valid (body 0002296127 → check 7).
        var (matched, valid) = Run("200002296127", 2000, 29);
        await Assert.That(matched).IsTrue();
        await Assert.That(valid).IsTrue();
    }

    [Test]
    public async Task InvalidLuhn_ReturnsFalse()
    {
        // ValidLong12 with check digit changed from 1 to 2 — Luhn-invalid.
        var (matched, valid) = Run("198901010102", 1989, 1);
        await Assert.That(matched).IsTrue();
        await Assert.That(valid).IsFalse();
    }

    [Test]
    public async Task DayZero_ReturnsFalse()
    {
        // realDay = 0 is not a valid calendar day; leaf must reject it.
        // (No production caller passes realDay = 0 — TSelf.IsDayValid pre-screens —
        // but the leaf must be safe by construction.)
        var (matched, valid) = Run(ValidLong12, 1989, 0);
        await Assert.That(matched).IsTrue();
        await Assert.That(valid).IsFalse();
    }
}
