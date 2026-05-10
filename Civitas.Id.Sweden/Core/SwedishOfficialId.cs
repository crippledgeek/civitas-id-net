using System.Globalization;
using System.Text.RegularExpressions;
using Civitas.Id.Sweden.Errors;
using Civitas.Id.Sweden.Format;
using JetBrains.Annotations;

namespace Civitas.Id.Sweden.Core;

/// <summary>
///     Common base for all Swedish official identification numbers.
///     Sealed sum type — only <c>PersonalId</c>, <c>CoordinationId</c>, and
///     <c>OrganisationId</c> derive from this type.
/// </summary>
[PublicAPI]
public abstract partial record SwedishOfficialId
{
    /// <summary>
    ///     Defensive upper bound on input length. No valid Swedish ID number representation
    ///     approaches this length (longest forms are 13 chars). The regex's 1-second match timeout
    ///     guards against catastrophic backtracking, but a cheap length check rejects pathological
    ///     inputs (e.g. 1MB of digits) before the regex engine is involved at all.
    /// </summary>
    private const int MaxInputLength = 100;

    /// <summary>Internal constructor — prevents external derivation.</summary>
    internal SwedishOfficialId()
    {
    }

    /// <summary>The ISO 3166-1 alpha-2 country code (always "SE" for this library).</summary>
    public static string CountryCode => "SE";

    [GeneratedRegex(
        @"^(?:SE)?(?<century>\d{2})?(?<date>(?<year>\d{2})(?<month>\d{2})(?<day>\d{2}))(?<delimiter>[-+])?(?<unique>\d{4})$",
        RegexOptions.None,
        1000)]
    private static partial Regex SsnRegex { get; }

    /// <summary>Returns the canonical 12-digit string representation.</summary>
    public abstract string LongFormat();

    /// <summary>Returns the canonical 10-digit string representation.</summary>
    public abstract string ShortFormat();

    /// <summary>Returns the ID formatted according to <paramref name="format" />.</summary>
    /// <param name="format">The desired output format.</param>
    public abstract string Format(PnrFormat format);

    /// <summary>
    ///     Attempts to parse <paramref name="s" /> as any Swedish official ID type.
    ///     Resolution order: <see cref="PersonalId" />, <see cref="CoordinationId" />, <see cref="OrganisationId" />.
    /// </summary>
    /// <param name="s">The ID string to parse.</param>
    /// <param name="result">The parsed instance when successful.</param>
    /// <returns>True when <paramref name="s" /> matches any supported Swedish ID format.</returns>
    [Pure]
    [ContractAnnotation("=> true, result: notnull; => false, result: null")]
    public static bool TryParseAny(
        string? s,
        out SwedishOfficialId? result)
    {
        if (PersonalId.TryParse(s, out var person))
        {
            result = person;
            return true;
        }

        if (CoordinationId.TryParse(s, out var coord))
        {
            result = coord;
            return true;
        }

        if (OrganisationId.TryParse(s, out var org))
        {
            result = org;
            return true;
        }

        result = null;
        return false;
    }

    /// <summary>
    ///     Parses <paramref name="s" /> as any Swedish official ID type. Throws on failure.
    /// </summary>
    /// <param name="s">The ID string to parse.</param>
    /// <returns>A valid Swedish official ID instance.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="s" /> is null.</exception>
    /// <exception cref="InvalidIdNumberException">When <paramref name="s" /> matches no supported Swedish ID format.</exception>
    [Pure]
    public static SwedishOfficialId ParseAny(string s)
    {
        ArgumentNullException.ThrowIfNull(s);
        return TryParseAny(s, out var result)
            ? result!
            : throw new InvalidIdNumberException(s, InvalidIdNumberReason.UnsupportedIdType);
    }

    /// <summary>
    ///     Attempts to parse <paramref name="s" /> as any Swedish official ID type. The
    ///     library converts <paramref name="timeProvider" />'s current UTC instant to
    ///     Sweden's civil timezone (<c>Europe/Stockholm</c>) and uses the resulting
    ///     year for century inference of two-digit-year inputs. Resolution order:
    ///     <see cref="PersonalId" />, <see cref="CoordinationId" />, <see cref="OrganisationId" />.
    /// </summary>
    /// <param name="s">The ID string to parse.</param>
    /// <param name="timeProvider">
    ///     The time provider used to determine the current UTC instant. The library
    ///     converts the UTC instant to Sweden's civil timezone (<c>Europe/Stockholm</c>)
    ///     to derive the current year for century inference; the provider's
    ///     <see cref="TimeProvider.LocalTimeZone" /> is intentionally ignored to
    ///     prevent host-timezone drift on cloud containers.
    /// </param>
    /// <param name="result">The parsed instance when successful.</param>
    /// <returns>True when <paramref name="s" /> matches any supported Swedish ID format.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="timeProvider" /> is <see langword="null" />.
    /// </exception>
    [Pure]
    [ContractAnnotation("=> true, result: notnull; => false, result: null")]
    public static bool TryParseAny(
        string? s,
        TimeProvider timeProvider,
        out SwedishOfficialId? result)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        if (PersonalId.TryParse(s, timeProvider, out var person))
        {
            result = person;
            return true;
        }

        if (CoordinationId.TryParse(s, timeProvider, out var coord))
        {
            result = coord;
            return true;
        }

        if (OrganisationId.TryParse(s, out var org))
        {
            result = org;
            return true;
        }

        result = null;
        return false;
    }

    /// <summary>
    ///     Parses <paramref name="s" /> as any Swedish official ID type. The library
    ///     converts <paramref name="timeProvider" />'s current UTC instant to Sweden's
    ///     civil timezone (<c>Europe/Stockholm</c>) and uses the resulting year for
    ///     century inference of two-digit-year inputs. Throws on failure.
    /// </summary>
    /// <param name="s">The ID string to parse.</param>
    /// <param name="timeProvider">
    ///     The time provider used to determine the current UTC instant. The library
    ///     converts the UTC instant to Sweden's civil timezone (<c>Europe/Stockholm</c>)
    ///     to derive the current year for century inference; the provider's
    ///     <see cref="TimeProvider.LocalTimeZone" /> is intentionally ignored to
    ///     prevent host-timezone drift on cloud containers.
    /// </param>
    /// <returns>A valid Swedish official ID instance.</returns>
    /// <exception cref="ArgumentNullException">
    ///     When <paramref name="s" /> or <paramref name="timeProvider" /> is null.
    /// </exception>
    /// <exception cref="InvalidIdNumberException">
    ///     When <paramref name="s" /> matches no supported Swedish ID format.
    /// </exception>
    [Pure]
    public static SwedishOfficialId ParseAny(string s, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(s);
        ArgumentNullException.ThrowIfNull(timeProvider);
        return TryParseAny(s, timeProvider, out var result)
            ? result!
            : throw new InvalidIdNumberException(s, InvalidIdNumberReason.UnsupportedIdType);
    }

    /// <summary>
    ///     Attempts to parse <paramref name="s" /> as any Swedish official ID type
    ///     using <paramref name="today" /> as the reference date for two-digit-year
    ///     century inference. Fully deterministic — does not read any clock.
    ///     Resolution order: <see cref="PersonalId" />, <see cref="CoordinationId" />,
    ///     <see cref="OrganisationId" />.
    /// </summary>
    /// <param name="s">The ID string to parse, or null.</param>
    /// <param name="today">The reference date for century inference.</param>
    /// <param name="result">The parsed instance on success, otherwise null.</param>
    /// <returns><see langword="true" /> if <paramref name="s" /> matches any supported Swedish ID format.</returns>
    [Pure]
    [ContractAnnotation("=> true, result: notnull; => false, result: null")]
    public static bool TryParseAny(
        string? s,
        DateOnly today,
        out SwedishOfficialId? result)
    {
        if (PersonalId.TryParse(s, today, out var person))
        {
            result = person;
            return true;
        }

        if (CoordinationId.TryParse(s, today, out var coord))
        {
            result = coord;
            return true;
        }

        if (OrganisationId.TryParse(s, today, out var org))
        {
            result = org;
            return true;
        }

        result = null;
        return false;
    }

    /// <inheritdoc cref="TryParseAny(string?, DateOnly, out SwedishOfficialId?)" />
    [Pure]
    [ContractAnnotation("=> true, result: notnull; => false, result: null")]
    public static bool TryParseAny(
        ReadOnlySpan<char> s,
        DateOnly today,
        out SwedishOfficialId? result)
    {
        if (PersonalId.TryParse(s, today, out var person))
        {
            result = person;
            return true;
        }

        if (CoordinationId.TryParse(s, today, out var coord))
        {
            result = coord;
            return true;
        }

        if (OrganisationId.TryParse(s, today, out var org))
        {
            result = org;
            return true;
        }

        result = null;
        return false;
    }

    /// <summary>
    ///     Parses <paramref name="s" /> as any Swedish official ID type using
    ///     <paramref name="today" /> as the reference date. Throws on failure.
    /// </summary>
    /// <param name="s">The ID string to parse.</param>
    /// <param name="today">The reference date for century inference.</param>
    /// <returns>A valid Swedish official ID instance.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="s" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidIdNumberException">
    ///     Thrown when <paramref name="s" /> matches no supported Swedish ID format.
    /// </exception>
    [Pure]
    public static SwedishOfficialId ParseAny(string s, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(s);
        return TryParseAny(s, today, out var result)
            ? result!
            : throw new InvalidIdNumberException(s, InvalidIdNumberReason.UnsupportedIdType);
    }

    /// <inheritdoc cref="ParseAny(string, DateOnly)" />
    [Pure]
    public static SwedishOfficialId ParseAny(ReadOnlySpan<char> s, DateOnly today) =>
        TryParseAny(s, today, out var result)
            ? result!
            : throw new InvalidIdNumberException(s.ToString(), InvalidIdNumberReason.UnsupportedIdType);

    /// <summary>
    ///     Returns true when <paramref name="s" /> is a valid personnummer, samordningsnummer, or organisationsnummer.
    /// </summary>
    /// <param name="s">The ID string to test.</param>
    /// <returns>True when the input matches any supported Swedish ID format.</returns>
    [Pure]
    public static bool IsValid(string? s)
    {
        return TryParseAny(s, out _);
    }

    /// <summary>
    ///     Pre-flight: trims and length-checks the input, then matches against the shared regex.
    ///     Returns null when the input is null, empty, too long, or non-matching.
    /// </summary>
    /// <param name="input">The raw string to test.</param>
    /// <returns>A <see cref="SwedishIdMatcher" /> when the input matches; otherwise null.</returns>
    private protected static SwedishIdMatcher? TryMatch(string? input)
    {
        if (input is null) return null;
        var trimmed = input.Trim();
        if (trimmed.Length is 0 or > MaxInputLength) return null;

        var match = SsnRegex.Match(trimmed);
        return match.Success ? new SwedishIdMatcher(match) : null;
    }

    /// <summary>Test-only shim — DO NOT use in production code.</summary>
    /// <remarks>
    ///     Exists because the real <c>TryMatch</c> is <c>private protected</c>;
    ///     this surface lets unit tests verify the matcher without granting broader visibility.
    ///     Marked <c>internal</c> so only the test assembly (via <c>InternalsVisibleTo</c>) can call it.
    /// </remarks>
    /// <param name="input">The raw string to test.</param>
    /// <returns>A <see cref="SwedishIdMatcher" /> when the input matches; otherwise null.</returns>
    internal static SwedishIdMatcher? TryMatchForTesting(string? input)
    {
        return TryMatch(input);
    }

    /// <summary>Wraps a successful regex match and exposes named-group accessors.</summary>
    internal sealed class SwedishIdMatcher
    {
        private readonly Match _match;

        /// <summary>Creates a new matcher wrapping the given successful <see cref="Match" />.</summary>
        /// <param name="match">A successful regex match against <see cref="SsnRegex" />.</param>
        internal SwedishIdMatcher(Match match)
        {
            _match = match;
        }

        /// <summary>True if the input contained an explicit century prefix (e.g. "19" or "20").</summary>
        public bool HasCentury => _match.Groups["century"].Success;

        /// <summary>True if the input contained a "-" or "+" delimiter.</summary>
        public bool HasDelimiter => _match.Groups["delimiter"].Success;

        /// <summary>The delimiter character ("" when absent).</summary>
        public string Delimiter => _match.Groups["delimiter"].Value;

        /// <summary>The 2-digit year text.</summary>
        public string YearText => _match.Groups["year"].Value;

        /// <summary>The 2-digit month text.</summary>
        public string MonthText => _match.Groups["month"].Value;

        /// <summary>The 2-digit day text.</summary>
        public string DayText => _match.Groups["day"].Value;

        /// <summary>The 4-digit "unique" suffix (last 3 + check digit).</summary>
        public string Unique => _match.Groups["unique"].Value;

        /// <summary>Century parsed as int.</summary>
        public int CenturyValue => int.Parse(_match.Groups["century"].Value, CultureInfo.InvariantCulture);

        /// <summary>Year parsed as int (0..99).</summary>
        public int Year => int.Parse(YearText, CultureInfo.InvariantCulture);

        /// <summary>Month parsed as int.</summary>
        public int Month => int.Parse(MonthText, CultureInfo.InvariantCulture);

        /// <summary>
        ///     Day parsed as int (1..31 for personnummer, 61..91 for samordningsnummer, &gt;=20 for organisationsnummer
        ///     "month").
        /// </summary>
        public int Day => int.Parse(DayText, CultureInfo.InvariantCulture);
    }
}
