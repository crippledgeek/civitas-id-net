using Civitas.Id.Sweden.Internal;
using Civitas.Id.Sweden.Tests.Helpers;

namespace Civitas.Id.Sweden.Tests.Internal;

/// <summary>
///     Tests for <c>SwedishIdParsing.TryValidatePersonShapedBody</c> — the
///     shared leaf atomic that performs DaysInMonth + Luhn validation for both the
///     person-ID dispatcher and OrganisationId's Enskild firma branch.
/// </summary>
public class TryValidatePersonShapedBodyTests
{
    private static readonly string ValidLong12 = OfficialNumbers.Modern.Long;

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
        var (matched, valid) = Run(ValidLong12, 2000, 1);
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
        // Feb 29 2000 — leap year.
        var leapDay = OfficialNumbers.Personnummer(new DateOnly(2000, 2, 29)).Long;
        var (matched, valid) = Run(leapDay, 2000, 29);
        await Assert.That(matched).IsTrue();
        await Assert.That(valid).IsTrue();
    }

    [Test]
    public async Task InvalidLuhn_ReturnsFalse()
    {
        var (matched, valid) = Run(OfficialNumbers.Modern.WithWrongCheckDigit, 2000, 1);
        await Assert.That(matched).IsTrue();
        await Assert.That(valid).IsFalse();
    }

    /// <summary>The year is the caller's, and a calendar type throws on those below one.</summary>
    [Test]
    [Arguments(int.MinValue, false)]
    [Arguments(-1, false)]
    [Arguments(0, false)]
    [Arguments(1799, false)]
    [Arguments(1800, true)]
    [Arguments(9999, true)]
    public async Task BirthYear_IsAcceptedFrom1800(int fullYear, bool expected)
    {
        var (matched, valid) = Run(ValidLong12, fullYear, 1);
        await Assert.That(matched).IsTrue();
        await Assert.That(valid).IsEqualTo(expected);
    }

    [Test]
    public async Task DayZero_ReturnsFalse()
    {
        // realDay = 0 is not a valid calendar day; leaf must reject it.
        // (No production caller passes realDay = 0 — TSelf.IsDayValid pre-screens —
        // but the leaf must be safe by construction.)
        var (matched, valid) = Run(ValidLong12, 2000, 0);
        await Assert.That(matched).IsTrue();
        await Assert.That(valid).IsFalse();
    }
}
