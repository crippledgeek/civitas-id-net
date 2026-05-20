using System.Diagnostics.CodeAnalysis;
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
    ///     Declared here rather than on <c>SwedishOfficialId</c> to keep it co-located with its sole consumer.
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
    ///     Allocation-free span-based equivalent of <see cref="TryMatch"/>.
    ///     Hand-rolled parser for the Swedish ID regular structure:
    ///     optional "SE" prefix (literal, case-sensitive), optional 2-digit century,
    ///     6 mandatory date digits (YYMMDD), optional '-'/'+' delimiter, 4 mandatory
    ///     unique/check digits. Equivalent to the <see cref="SwedishOfficialId.SsnRegex"/>
    ///     pattern but eliminates the Match/Group object graph (~1 KB per parse).
    /// </summary>
    /// <param name="input">The candidate input.</param>
    /// <param name="result">The parsed span match on success.</param>
    /// <returns><see langword="true"/> if the input matches the structural pattern.</returns>
    [Pure]
    internal static bool TryMatchSpan(ReadOnlySpan<char> input, out SwedishIdSpanMatch result)
    {
        result = default;
        if (input.IsEmpty) return false;

        var trimmed = input.Trim();
        if (trimmed.Length is 0 or > MaxInputLength) return false;

        var bodyStart = 0;

        // Optional literal "SE" prefix (case-sensitive — matches the regex literal).
        if (trimmed.Length >= 2 && trimmed[0] == 'S' && trimmed[1] == 'E')
        {
            bodyStart = 2;
        }

        var bodyLen = trimmed.Length - bodyStart;

        // After SE prefix, body must be one of (lengths):
        //   10 — YYMMDDNNNN
        //   11 — YYMMDD?NNNN (delimiter present, no century)
        //   12 — YYYYMMDDNNNN
        //   13 — YYYYMMDD?NNNN (century + delimiter)
        if (bodyLen is < 10 or > 13) return false;

        // Delimiter, if present, sits exactly 4 chars from the end (just before the 4-digit unique).
        // bodyLen ≥ 10 (guarded above) implies delimIdxInBody ≥ 5, so no further bound check needed.
        var delimIdxInBody = bodyLen - 5;
        var delimChar = trimmed[bodyStart + delimIdxInBody];
        var hasDelimiter = delimChar is '-' or '+';
        var delimAdjust = hasDelimiter ? 1 : 0;

        // Effective digit-only length: must be exactly 10 (no century) or 12 (with century).
        var effectiveLen = bodyLen - delimAdjust;
        if (effectiveLen is not (10 or 12)) return false;
        var hasCentury = effectiveLen == 12;

        var p = bodyStart;

        var centuryValue = 0;
        if (hasCentury)
        {
            if (!IsDigit(trimmed[p]) || !IsDigit(trimmed[p + 1])) return false;
            centuryValue = (trimmed[p] - '0') * 10 + (trimmed[p + 1] - '0');
            p += 2;
        }

        // YYMMDD — 6 digits
        var yearStart = p;
        var monthStart = p + 2;
        var dayStart = p + 4;
        for (var k = 0; k < 6; k++)
            if (!IsDigit(trimmed[p + k])) return false;
        var year = (trimmed[p] - '0') * 10 + (trimmed[p + 1] - '0');
        var month = (trimmed[p + 2] - '0') * 10 + (trimmed[p + 3] - '0');
        var day = (trimmed[p + 4] - '0') * 10 + (trimmed[p + 5] - '0');
        p += 6;

        var delimiter = '\0';
        if (hasDelimiter)
        {
            delimiter = trimmed[p];
            p++;
        }

        // 4-digit unique
        var uniqueStart = p;
        for (var k = 0; k < 4; k++)
            if (!IsDigit(trimmed[p + k])) return false;
        p += 4;

        if (p != trimmed.Length) return false;

        result = new SwedishIdSpanMatch
        {
            Source = trimmed,
            HasCentury = hasCentury,
            CenturyValue = centuryValue,
            Year = year,
            Month = month,
            Day = day,
            Delimiter = delimiter,
            YearStart = yearStart,
            MonthStart = monthStart,
            DayStart = dayStart,
            UniqueStart = uniqueStart
        };
        return true;
    }

    private static bool IsDigit(char c) => (uint)(c - '0') <= 9;

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
        if (realDay < 1 || realDay > DateTime.DaysInMonth(fullYear, matcher.Month)) return false;

        Span<char> tenDigits = stackalloc char[10];
        matcher.YearTextSpan.CopyTo(tenDigits[..2]);
        matcher.MonthTextSpan.CopyTo(tenDigits[2..4]);
        matcher.DayTextSpan.CopyTo(tenDigits[4..6]);
        matcher.UniqueSpan.CopyTo(tenDigits[6..10]);
        return SwedishLuhnAlgorithm.IsValid(tenDigits);
    }

    /// <summary>
    ///     Span-based equivalent of
    ///     <see cref="TryValidatePersonShapedBody(SwedishOfficialId.SwedishIdMatcher, int, int)"/>.
    ///     Validates calendar-day-within-month and Luhn-10 over the 10-digit body.
    /// </summary>
    /// <param name="match">The span match containing raw year/month/day/unique spans.</param>
    /// <param name="fullYear">The 4-digit year (caller-resolved).</param>
    /// <param name="realDay">The calendar day (1..31).</param>
    /// <returns><see langword="true"/> if both calendar and Luhn checks pass.</returns>
    [Pure]
    internal static bool TryValidatePersonShapedBody(
        in SwedishIdSpanMatch match, int fullYear, int realDay)
    {
        if (realDay < 1 || realDay > DateTime.DaysInMonth(fullYear, match.Month)) return false;

        Span<char> tenDigits = stackalloc char[10];
        match.YearTextSpan.CopyTo(tenDigits[..2]);
        match.MonthTextSpan.CopyTo(tenDigits[2..4]);
        match.DayTextSpan.CopyTo(tenDigits[4..6]);
        match.UniqueSpan.CopyTo(tenDigits[6..10]);
        return SwedishLuhnAlgorithm.IsValid(tenDigits);
    }

    /// <summary>
    ///     Parses an organisation number (10 or 12 digits per Lag (1974:174) §4),
    ///     accepting both legal-person and Enskild firma (sole-proprietor) shapes.
    /// </summary>
    /// <param name="s">The candidate input, or null.</param>
    /// <param name="result">The parsed organisation ID on success.</param>
    /// <returns><see langword="true"/> on success.</returns>
    /// <remarks>
    ///     Migrated from <c>OrganisationId.TryParse</c>.
    ///     OrganisationId is constructed via its internal factory.
    /// </remarks>
    [Pure]
    [ContractAnnotation("=> true, result: notnull; => false, result: null")]
    internal static bool TryParseOrganisation(
        string? s, [MaybeNullWhen(false)] out OrganisationId result)
    {
        result = null;
        if (s is null) return false;
        if (!TryMatchSpan(s.AsSpan(), out var match)) return false;

        // PeOrgNr "16" prefix is for legal-person 12-digit input only — reject for person-shape.
        // "161212121212" (month 12, day 12, century 16) is not a real birth year (no one born in 1600s).
        // "16" can only appear as input prefix for legal-person orgnummer (month >= 20).
        if (match is { HasCentury: true, CenturyValue: 16, Month: < 20 })
            return false;

        var month = match.Month;
        var day = match.Day;
        int? personCentury = null;
        var realDay = day;
        var isEnskildFirma = false;

        switch (month)
        {
            case >= 20:
                // Legal-person orgnummer — month/day are not calendar values.
                // For 12-digit form, century must be "16" (legacy prefix).
                if (match.HasCentury && match.CenturyValue != 16) return false;
                break;
            case >= 1 and <= 12 when day is >= 1 and <= 31:
                // Enskild firma — personnummer date shape. Require explicit century.
                if (!match.HasCentury) return false;
                personCentury = match.CenturyValue;
                isEnskildFirma = true;
                break;
            case >= 1 and <= 12 when day is >= 61 and <= 91:
                // Enskild firma — samordningsnummer day-offset shape. Require explicit century.
                if (!match.HasCentury) return false;
                personCentury = match.CenturyValue;
                isEnskildFirma = true;
                realDay = day - 60;
                break;
            default:
                // Anything else (e.g. month 13-19) is invalid for any form.
                return false;
        }

        if (isEnskildFirma)
        {
            var fullYear = personCentury!.Value * 100 + match.Year;
            // Shared leaf atomic — calendar-day + Luhn in one call.
            if (!TryValidatePersonShapedBody(in match, fullYear, realDay)) return false;
        }
        else
        {
            // Legal-person branch — Luhn only, no calendar-date check.
            Span<char> tenDigits = stackalloc char[10];
            match.YearTextSpan.CopyTo(tenDigits[..2]);
            match.MonthTextSpan.CopyTo(tenDigits[2..4]);
            match.DayTextSpan.CopyTo(tenDigits[4..6]);
            match.UniqueSpan.CopyTo(tenDigits[6..10]);
            if (!SwedishLuhnAlgorithm.IsValid(tenDigits)) return false;
        }

        // Build canonical 10-digit form regardless of branch.
        Span<char> canonical = stackalloc char[10];
        match.YearTextSpan.CopyTo(canonical[..2]);
        match.MonthTextSpan.CopyTo(canonical[2..4]);
        match.DayTextSpan.CopyTo(canonical[4..6]);
        match.UniqueSpan.CopyTo(canonical[6..10]);

        result = OrganisationId.FromValidated(new string(canonical), personCentury);
        return true;
    }
}
