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
    ///     Thin accessor wrapping <see cref="SwedishOfficialId"/>'s
    ///     <c>[GeneratedRegex]</c> match. Co-located here so person-ID and
    ///     organisation-ID dispatchers share one entry point. The regex declaration
    ///     itself stays on <see cref="SwedishOfficialId"/> per Roslyn's
    ///     <c>[GeneratedRegex]</c> co-location requirement.
    /// </summary>
    /// <param name="s">The candidate ID string (any supported format), or null.</param>
    /// <returns>The parsed matcher on regex success, or null otherwise.</returns>
    internal static SwedishOfficialId.SwedishIdMatcher? TryMatch(string? s) => SwedishOfficialId.MatchForInternal(s);

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
