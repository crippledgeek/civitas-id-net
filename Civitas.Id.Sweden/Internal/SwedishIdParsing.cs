using Civitas.Id.Sweden.Core;
using JetBrains.Annotations;

namespace Civitas.Id.Sweden.Internal;

/// <summary>
///     Shared parsing primitives for personnummer / samordningsnummer.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="ResolveCentury" /> centralises the sliding-100-year-window century
///         inference that <see cref="Core.PersonalId" /> and <see cref="Core.CoordinationId" />
///         share textually. Inference is time-dependent: a 10-digit ID parsed near a year
///         boundary could resolve to different centuries on different days. For
///         deterministic parsing, prefer the 12-digit form with explicit century, or wait
///         for the planned <c>Parse(string, DateOnly today)</c> overload.
///     </para>
/// </remarks>
internal static class SwedishIdParsing
{
    /// <summary>
    ///     Resolves the century for a parsed ID. If <paramref name="explicitCentury" />
    ///     is provided, uses it directly. Otherwise infers from
    ///     <paramref name="currentYear" /> using a sliding 100-year window, with a
    ///     further adjustment for the <c>'+'</c> separator (person is 100+ years old).
    /// </summary>
    /// <param name="explicitCentury">
    ///     The 2-digit explicit century if the input had one (e.g. 19 from "192506010004"); null if
    ///     absent.
    /// </param>
    /// <param name="twoDigitYear">The 2-digit short year from the input.</param>
    /// <param name="currentYear">
    ///     The reference year (typically Sweden civil today's year) used as anchor for sliding-window
    ///     inference.
    /// </param>
    /// <param name="plusSeparator">
    ///     True if the input had a '+' separator (centenarian convention); false for '-' or no
    ///     separator.
    /// </param>
    /// <returns>The resolved century (e.g. 19 or 20).</returns>
    public static int ResolveCentury(int? explicitCentury, int twoDigitYear, int currentYear, bool plusSeparator)
    {
        if (explicitCentury is { } c) return c;

        var currentCentury = currentYear / 100;
        var inferredYear = currentCentury * 100 + twoDigitYear;

        // If inferred year is in the future, the person is from the previous century
        if (inferredYear > currentYear) inferredYear -= 100;

        var century = inferredYear / 100;

        // '+' separator means person is 100+ years old, shift back another century
        if (plusSeparator)
            century -= 1;

        return century;
    }

    /// <summary>
    ///     Defensive upper bound on input length. No valid Swedish ID number representation
    ///     approaches this length (longest forms are 13 chars). The regex's 1-second match timeout
    ///     guards against catastrophic backtracking, but a cheap length check rejects pathological
    ///     inputs (e.g. 1MB of digits) before the regex engine is involved at all.
    /// </summary>
    private const int MaxInputLength = 100;

    /// <summary>
    ///     Pre-flight: trims and length-checks the input, then matches against the shared regex
    ///     declared on <see cref="SwedishOfficialId"/>. Returns null when the input is null,
    ///     empty, too long, or non-matching. Co-located here so person-ID and organisation-ID
    ///     dispatchers share one entry point. The <c>[GeneratedRegex]</c> partial property
    ///     itself stays on <see cref="SwedishOfficialId"/> per Roslyn's source-generator
    ///     co-location requirement (dotnet/runtime#63502).
    /// </summary>
    /// <param name="input">The candidate ID string (any supported format), or null.</param>
    /// <returns>A <see cref="SwedishOfficialId.SwedishIdMatcher"/> when the input matches; otherwise null.</returns>
    [Pure]
    internal static SwedishOfficialId.SwedishIdMatcher? TryMatch(string? input)
    {
        if (input is null) return null;
        var trimmed = input.Trim();
        if (trimmed.Length is 0 or > MaxInputLength) return null;

        var match = SwedishOfficialId.SsnRegex.Match(trimmed);
        return match.Success ? new SwedishOfficialId.SwedishIdMatcher(match) : null;
    }

    /// <summary>
    ///     Shared leaf atomic: validates calendar-day-within-month plus Luhn-10 over
    ///     the 10-digit body. Used by both the person-ID dispatcher
    ///     (Tasks 3, 4, 7) and the Enskild firma branch of OrganisationId parsing
    ///     (Tasks 5, 11). Caller has already resolved <paramref name="fullYear"/>
    ///     and <paramref name="realDay"/>.
    /// </summary>
    /// <param name="matcher">The matcher containing raw year/month/day/unique spans.</param>
    /// <param name="fullYear">The 4-digit year (caller-resolved).</param>
    /// <param name="realDay">
    ///     The calendar day (1..31). For samordningsnummer the caller passes
    ///     <c>encodedDay - 60</c>; for personnummer the encoded day directly.
    /// </param>
    /// <returns><see langword="true"/> if both calendar and Luhn checks pass.</returns>
    [Pure]
    internal static bool TryValidatePersonShapedBody(
        SwedishOfficialId.SwedishIdMatcher matcher, int fullYear, int realDay)
    {
        if (realDay > DateTime.DaysInMonth(fullYear, matcher.Month)) return false;

        Span<char> tenDigits = stackalloc char[10];
        matcher.YearText.AsSpan().CopyTo(tenDigits[..2]);
        matcher.MonthText.AsSpan().CopyTo(tenDigits[2..4]);
        matcher.DayText.AsSpan().CopyTo(tenDigits[4..6]);
        matcher.Unique.AsSpan().CopyTo(tenDigits[6..10]);
        return SwedishLuhnAlgorithm.IsValid(tenDigits);
    }
}
