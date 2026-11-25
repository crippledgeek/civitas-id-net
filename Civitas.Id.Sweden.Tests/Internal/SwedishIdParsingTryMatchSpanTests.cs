using Civitas.Id.Sweden.Internal;
using Civitas.Id.Sweden.Tests.Helpers;

namespace Civitas.Id.Sweden.Tests.Internal;

/// <summary>
///     Tests for <see cref="SwedishIdParsing.TryMatchSpan" /> — the allocation-free
///     hand-rolled parser that replaces the legacy <c>[GeneratedRegex]</c> path.
///     Preserves the 10 scenarios previously covered by the reference-type
///     <c>SwedishIdMatcher</c> tests (null / empty / length cap / SE prefix /
///     whitespace trim / 12-digit / 10-digit / hyphen / plus / invalid chars +
///     the 100-char boundary).
/// </summary>
public class SwedishIdParsingTryMatchSpanTests
{
    // Born on 15 December 1981; the day part is 75.
    private static readonly OfficialSamordningsnummer Number = OfficialNumbers.TwentiethCenturySamordningsnummer;

    /// <summary>
    ///     Snapshot of a <see cref="SwedishIdSpanMatch"/> into plain fields so the
    ///     test method can <see langword="await"/> assertions afterwards. The
    ///     match itself is a <c>ref struct</c> that cannot cross an
    ///     <see langword="await"/> boundary (CS4007).
    /// </summary>
    private readonly record struct MatchSnapshot(
        bool Ok,
        bool HasCentury,
        int CenturyValue,
        int Year,
        int Month,
        int Day,
        char Delimiter,
        string Unique);

    private static MatchSnapshot Snapshot(string? input)
    {
        var span = input is null ? default : input.AsSpan();
        if (!SwedishIdParsing.TryMatchSpan(span, out var m))
            return new MatchSnapshot(false, false, 0, 0, 0, 0, '\0', string.Empty);
        return new MatchSnapshot(
            true,
            m.HasCentury,
            m.CenturyValue,
            m.Year,
            m.Month,
            m.Day,
            m.Delimiter,
            new string(m.UniqueSpan));
    }

    [Test]
    public async Task TryMatchSpan_NullInput_ReturnsFalse()
    {
        // ReadOnlySpan<char> has no `null` — `default` IS the empty span,
        // which the parser treats equivalently to an empty/whitespace string.
        var snap = Snapshot(null);
        await Assert.That(snap.Ok).IsFalse();
    }

    [Test]
    public async Task TryMatchSpan_Empty_ReturnsFalse()
    {
        await Assert.That(Snapshot("").Ok).IsFalse();
        await Assert.That(Snapshot("   ").Ok).IsFalse();
    }

    [Test]
    public async Task TryMatchSpan_TooLong_ReturnsFalse()
    {
        await Assert.That(Snapshot(new string('1', 101)).Ok).IsFalse();
    }

    [Test]
    public async Task TryMatchSpan_TwelveDigit_CapturesCenturyAndDate()
    {
        var snap = Snapshot(Number.Long);
        await Assert.That(snap.Ok).IsTrue();
        await Assert.That(snap.HasCentury).IsTrue();
        await Assert.That(snap.CenturyValue).IsEqualTo(19);
        await Assert.That(snap.Year).IsEqualTo(81);
        await Assert.That(snap.Month).IsEqualTo(12);
        await Assert.That(snap.Day).IsEqualTo(75);
        await Assert.That(snap.Unique).IsEqualTo(Number.Serial);
    }

    [Test]
    public async Task TryMatchSpan_TenDigitNoSeparator_CapturesNoCentury()
    {
        var snap = Snapshot(Number.Short);
        await Assert.That(snap.Ok).IsTrue();
        await Assert.That(snap.HasCentury).IsFalse();
        await Assert.That(snap.Delimiter).IsEqualTo('\0');
    }

    [Test]
    public async Task TryMatchSpan_TenDigitWithSeparator_CapturesDelimiter()
    {
        var snap = Snapshot(Number.ShortWithHyphen);
        await Assert.That(snap.Ok).IsTrue();
        await Assert.That(snap.Delimiter).IsEqualTo('-');
    }

    [Test]
    public async Task TryMatchSpan_PlusSeparator_CapturedAsDelimiter()
    {
        var snap = Snapshot(OfficialNumbers.Centenarian.ShortWithPlus);
        await Assert.That(snap.Ok).IsTrue();
        await Assert.That(snap.Delimiter).IsEqualTo('+');
    }

    [Test]
    public async Task TryMatchSpan_NonNumeric_ReturnsFalse()
    {
        await Assert.That(Snapshot("abcdefghij").Ok).IsFalse();
    }

    [Test]
    public async Task TryMatchSpan_WithSePrefix_Matches()
    {
        var snap = Snapshot($"SE{Number.Long}");
        await Assert.That(snap.Ok).IsTrue();
        await Assert.That(snap.HasCentury).IsTrue();
        await Assert.That(snap.CenturyValue).IsEqualTo(19);
    }

    [Test]
    public async Task TryMatchSpan_LeadingTrailingWhitespace_Matches()
    {
        var snap = Snapshot($"  {Number.Long}  ");
        await Assert.That(snap.Ok).IsTrue();
        await Assert.That(snap.Year).IsEqualTo(81);
    }

    [Test]
    public async Task TryMatchSpan_Length100Boundary_FailsStructuralMatch()
    {
        // Length is exactly at MaxInputLength (100) — passes the length guard
        // but the structural shape check rejects it (no valid Swedish ID has
        // 100 digit-only chars). Pins the boundary.
        await Assert.That(Snapshot(new string('1', 100)).Ok).IsFalse();
    }
}
