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
    ///     approaches this length (longest forms are 13 chars). A cheap length check rejects
    ///     pathological inputs (e.g. 1MB of digits) before any per-character work runs.
    ///     Declared here rather than on <c>SwedishOfficialId</c> to keep it co-located with its sole consumer.
    /// </summary>
    private const int MaxInputLength = 100;

    /// <summary>
    ///     Allocation-free hand-rolled structural parser for the Swedish ID grammar:
    ///     optional "SE" prefix (literal, case-sensitive), optional 2-digit century,
    ///     6 mandatory date digits (YYMMDD), optional '-'/'+' delimiter, 4 mandatory
    ///     unique/check digits. Co-located here so person-ID and organisation-ID
    ///     dispatchers share one entry point.
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

        // ─── Optional literal "SE" prefix (case-sensitive — matches the regex literal) ───
        var bodyStart = StartsWithSePrefix(trimmed) ? 2 : 0;
        var bodyLen = trimmed.Length - bodyStart;

        // After SE prefix, body must be one of (lengths):
        //   10 — YYMMDDNNNN
        //   11 — YYMMDD?NNNN (delimiter present, no century)
        //   12 — YYYYMMDDNNNN
        //   13 — YYYYMMDD?NNNN (century + delimiter)
        if (bodyLen is < 10 or > 13) return false;

        // ─── Delimiter (if present) sits exactly 4 chars from the end ───
        // bodyLen ≥ 10 (guarded above) implies delimiter position ≥ 5, so no further bound check needed.
        var hasDelimiter = TryReadDelimiter(trimmed, bodyStart + bodyLen - 5, out var delimiter);
        var effectiveLen = bodyLen - (hasDelimiter ? 1 : 0);

        // ─── Determine grammar shape from digit-only length: 10 (no century) or 12 (with century) ───
        bool hasCentury;
        switch (effectiveLen)
        {
            case 10: hasCentury = false; break;
            case 12: hasCentury = true; break;
            default: return false;
        }

        var p = bodyStart;

        // ─── Optional 2-digit century ───
        var centuryValue = 0;
        if (hasCentury)
        {
            if (!TryReadTwoDigits(trimmed, p, out centuryValue)) return false;
            p += 2;
        }

        // ─── YYMMDD — 6 mandatory digits ───
        var yearStart = p;
        if (!TryReadTwoDigits(trimmed, p, out var year)) return false;
        p += 2;
        var monthStart = p;
        if (!TryReadTwoDigits(trimmed, p, out var month)) return false;
        p += 2;
        var dayStart = p;
        if (!TryReadTwoDigits(trimmed, p, out var day)) return false;
        p += 2;

        // ─── Optional delimiter (already detected above; just advance past it) ───
        if (hasDelimiter) p++;

        // ─── 4-digit unique suffix ───
        var uniqueStart = p;
        if (!TryValidateDigits(trimmed, p, 4)) return false;
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

    /// <summary>
    ///     Attempts to read exactly two ASCII digits at <paramref name="offset"/>
    ///     into <paramref name="value"/> (0..99).
    /// </summary>
    [Pure]
    private static bool TryReadTwoDigits(ReadOnlySpan<char> span, int offset, out int value)
    {
        if ((uint)(offset + 1) < (uint)span.Length
            && char.IsAsciiDigit(span[offset])
            && char.IsAsciiDigit(span[offset + 1]))
        {
            value = (span[offset] - '0') * 10 + (span[offset + 1] - '0');
            return true;
        }
        value = 0;
        return false;
    }

    /// <summary>
    ///     Attempts to read exactly <paramref name="count"/> ASCII digits at
    ///     <paramref name="offset"/>. Validates digit characters; does not decode
    ///     the numeric value (callers that need positional spans take this path).
    /// </summary>
    [Pure]
    private static bool TryValidateDigits(ReadOnlySpan<char> span, int offset, int count)
    {
        if ((uint)(offset + count) > (uint)span.Length) return false;
        for (var i = 0; i < count; i++)
        {
            if (!char.IsAsciiDigit(span[offset + i])) return false;
        }
        return true;
    }

    /// <summary>
    ///     Attempts to read a '-' or '+' delimiter at <paramref name="offset"/>.
    ///     Returns <see langword="false"/> with <paramref name="delimiter"/> = '\0'
    ///     when the position is past end or holds a non-delimiter character.
    /// </summary>
    [Pure]
    private static bool TryReadDelimiter(ReadOnlySpan<char> span, int offset, out char delimiter)
    {
        if ((uint)offset < (uint)span.Length
            && (span[offset] == '-' || span[offset] == '+'))
        {
            delimiter = span[offset];
            return true;
        }
        delimiter = '\0';
        return false;
    }

    /// <summary>
    ///     Returns <see langword="true"/> if <paramref name="span"/> starts with
    ///     the literal "SE" prefix (case-sensitive per Swedish convention).
    /// </summary>
    [Pure]
    private static bool StartsWithSePrefix(ReadOnlySpan<char> span)
        => span.Length >= 2 && span[0] == 'S' && span[1] == 'E';

    /// <summary>
    ///     Shared leaf atomic: validates calendar-day-within-month plus Luhn-10 over
    ///     the 10-digit body. Used by both the person-ID dispatcher and the Enskild
    ///     firma branch of OrganisationId parsing. Caller has already resolved
    ///     <paramref name="fullYear"/> and <paramref name="realDay"/>.
    /// </summary>
    /// <param name="match">The span match containing raw year/month/day/unique spans.</param>
    /// <param name="fullYear">The 4-digit year (caller-resolved).</param>
    /// <param name="realDay">
    ///     The calendar day (1..31). For samordningsnummer the caller passes
    ///     <c>encodedDay - 60</c>; for personnummer the encoded day directly.
    /// </param>
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
