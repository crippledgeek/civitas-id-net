using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Civitas.Id.Sweden.Errors;
using Civitas.Id.Sweden.Format;
using Civitas.Id.Sweden.Internal;
using Civitas.Id.Sweden.TypeConverters;
using JetBrains.Annotations;

namespace Civitas.Id.Sweden.Core;

/// <summary>
///     A Swedish coordination number (samordningsnummer).
///     Issued to individuals without a Swedish personnummer; the day field is offset by +60.
/// </summary>
[TypeConverter(typeof(CoordinationIdTypeConverter))]
public sealed record CoordinationId : PhysicalPersonId,
    ISpanParsable<CoordinationId>
{
    /// <summary>The canonical 12-digit normalised form (YYYYMMDDXXXX) with day still +60-offset.</summary>
    private readonly string _normalised;

    private CoordinationId(string normalised)
    {
        _normalised = normalised;
    }

    /// <summary>The decoded date of birth (encoded day minus 60).</summary>
    public override DateOnly BirthDate
    {
        get
        {
            var year = int.Parse(_normalised.AsSpan(0, 4), CultureInfo.InvariantCulture);
            var month = int.Parse(_normalised.AsSpan(4, 2), CultureInfo.InvariantCulture);
            var encodedDay = int.Parse(_normalised.AsSpan(6, 2), CultureInfo.InvariantCulture);
            return new DateOnly(year, month, encodedDay - 60);
        }
    }

    /// <inheritdoc />
    protected override char GenderDigit => _normalised[10];

    // ── IParsable<CoordinationId> contract ──

    /// <inheritdoc cref="IParsable{TSelf}.Parse(string, IFormatProvider?)" />
    static CoordinationId IParsable<CoordinationId>.Parse(string s, IFormatProvider? provider)
    {
        return Parse(s);
    }

    /// <inheritdoc cref="IParsable{TSelf}.TryParse(string?, IFormatProvider?, out TSelf)" />
    static bool IParsable<CoordinationId>.TryParse(
        string? s, IFormatProvider? provider,
        [MaybeNullWhen(false)] out CoordinationId result)
    {
        return TryParse(s, out result);
    }

    // ── ISpanParsable<CoordinationId> contract ──

    /// <summary>Parses a samordningsnummer span. Throws on failure.</summary>
    /// <param name="s">The ID span to parse.</param>
    /// <param name="provider">Format provider — accepted and ignored (Swedish IDs are culture-invariant).</param>
    /// <returns>A valid <see cref="CoordinationId" />.</returns>
    /// <exception cref="InvalidIdNumberException">When <paramref name="s" /> is not a valid samordningsnummer.</exception>
    [Pure]
    public static CoordinationId Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        return TryParse(s, provider, out var result)
            ? result
            : throw new InvalidIdNumberException(s.ToString(), InvalidIdNumberReason.InvalidFormat);
    }

    /// <summary>Attempts to parse a samordningsnummer span.</summary>
    /// <param name="s">The ID span to parse.</param>
    /// <param name="provider">Format provider — accepted and ignored.</param>
    /// <param name="result">The parsed <see cref="CoordinationId" /> when successful.</param>
    /// <returns>True when parsing succeeds.</returns>
    [Pure]
    [ContractAnnotation("=> true, result: notnull; => false, result: null")]
    public static bool TryParse(
        ReadOnlySpan<char> s, IFormatProvider? provider,
        [MaybeNullWhen(false)] out CoordinationId result)
    {
        return TryParse(s.ToString(), out result);
    }

    /// <summary>
    ///     Internal factory: constructs a <see cref="CoordinationId" /> from an already-validated
    ///     12-digit normalised string. Skips re-parsing — caller MUST guarantee validity
    ///     (correct length, valid Luhn, valid calendar date with day +60-offset).
    /// </summary>
    /// <param name="normalised12">A canonical 12-digit normalised samordningsnummer (YYYYMMDDXXXX, day still +60-offset).</param>
    /// <returns>A <see cref="CoordinationId" /> wrapping the provided digits.</returns>
    internal static CoordinationId FromValidated(string normalised12)
    {
        Debug.Assert(normalised12.Length == 12);
        return new CoordinationId(normalised12);
    }

    /// <summary>
    ///     Returns this samordningsnummer viewed as a sole-proprietor organisation number.
    /// </summary>
    /// <returns>An <see cref="OrganisationId" /> with <see cref="OrganisationNumberType.PhysicalPerson" />.</returns>
    [Pure]
    public OrganisationId ToOrganisationId()
    {
        return OrganisationId.FromPhysicalPersonId(
            ShortFormat(),
            int.Parse(_normalised.AsSpan(0, 2), CultureInfo.InvariantCulture));
    }

    /// <summary>Parses a samordningsnummer string. Throws <see cref="InvalidIdNumberException" /> on failure.</summary>
    /// <param name="s">The input string to parse.</param>
    /// <returns>A <see cref="CoordinationId" /> instance.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="s" /> is null.</exception>
    /// <exception cref="InvalidIdNumberException">When <paramref name="s" /> is not a valid samordningsnummer.</exception>
    [Pure]
    public static CoordinationId Parse(string s)
    {
        ArgumentNullException.ThrowIfNull(s);
        return TryParse(s, out var result)
            ? result
            : throw new InvalidIdNumberException(s, InvalidIdNumberReason.InvalidFormat);
    }

    /// <summary>Attempts to parse a samordningsnummer string.</summary>
    /// <param name="s">The input string to parse, or null.</param>
    /// <param name="result">On success, the parsed <see cref="CoordinationId" />; otherwise null.</param>
    /// <returns><c>true</c> when parsing succeeded; otherwise <c>false</c>.</returns>
    [Pure]
    [ContractAnnotation("=> true, result: notnull; => false, result: null")]
    public static bool TryParse(
        string? s,
        [MaybeNullWhen(false)] out CoordinationId result)
    {
        return TryParseCore(s, SwedenClock.Today().Year, out result);
    }

    /// <summary>
    ///     Parses a samordningsnummer using <paramref name="timeProvider" /> for century
    ///     inference of two-digit-year inputs.
    /// </summary>
    /// <param name="s">The samordningsnummer string to parse.</param>
    /// <param name="timeProvider">
    ///     The time provider used to determine the current UTC instant. The library
    ///     converts the UTC instant to Sweden's civil timezone (<c>Europe/Stockholm</c>)
    ///     to derive the current year for century inference; the provider's
    ///     <see cref="TimeProvider.LocalTimeZone" /> is intentionally ignored to
    ///     prevent host-timezone drift on cloud containers.
    /// </param>
    /// <returns>A <see cref="CoordinationId" /> instance.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="s" /> or <paramref name="timeProvider" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidIdNumberException">
    ///     Thrown when <paramref name="s" /> is not a valid samordningsnummer.
    /// </exception>
    [Pure]
    public static CoordinationId Parse(string s, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(s);
        var currentYear = SwedenClock.Today(timeProvider).Year;
        return TryParseCore(s, currentYear, out var result)
            ? result
            : throw new InvalidIdNumberException(s, InvalidIdNumberReason.InvalidFormat);
    }

    /// <summary>
    ///     Tries to parse a samordningsnummer using <paramref name="timeProvider" /> for
    ///     century inference of two-digit-year inputs.
    /// </summary>
    /// <param name="s">The samordningsnummer string to parse, or null.</param>
    /// <param name="timeProvider">
    ///     The time provider used to determine the current UTC instant. The library
    ///     converts the UTC instant to Sweden's civil timezone (<c>Europe/Stockholm</c>)
    ///     to derive the current year for century inference; the provider's
    ///     <see cref="TimeProvider.LocalTimeZone" /> is intentionally ignored to
    ///     prevent host-timezone drift on cloud containers.
    /// </param>
    /// <param name="result">The parsed samordningsnummer on success, otherwise null.</param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="timeProvider" /> is <see langword="null" />.
    /// </exception>
    [Pure]
    [ContractAnnotation("=> true, result: notnull; => false, result: null")]
    public static bool TryParse(
        string? s,
        TimeProvider timeProvider,
        [MaybeNullWhen(false)] out CoordinationId result)
    {
        var currentYear = SwedenClock.Today(timeProvider).Year;
        return TryParseCore(s, currentYear, out result);
    }

    /// <summary>
    ///     Parses a samordningsnummer using <paramref name="today" /> as the reference
    ///     date for two-digit-year century inference. Fully deterministic —
    ///     does not read any clock.
    /// </summary>
    /// <param name="s">The samordningsnummer string to parse.</param>
    /// <param name="today">
    ///     The reference date for century inference. Use this overload when you
    ///     have an explicit "as-of" date (data migrations, batch processing,
    ///     deterministic test fixtures). For live applications that read the
    ///     wall clock, prefer <see cref="Parse(string, TimeProvider)" />.
    /// </param>
    /// <returns>A <see cref="CoordinationId" /> instance.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="s" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidIdNumberException">
    ///     Thrown when <paramref name="s" /> is not a valid samordningsnummer.
    /// </exception>
    [Pure]
    public static CoordinationId Parse(string s, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(s);
        return TryParseCore(s, today.Year, out var result)
            ? result
            : throw new InvalidIdNumberException(s, InvalidIdNumberReason.InvalidFormat);
    }

    /// <summary>
    ///     Tries to parse a samordningsnummer using <paramref name="today" /> as the
    ///     reference date for two-digit-year century inference. Fully
    ///     deterministic — does not read any clock.
    /// </summary>
    /// <param name="s">The samordningsnummer string to parse, or null.</param>
    /// <param name="today">The reference date for century inference.</param>
    /// <param name="result">The parsed samordningsnummer on success, otherwise null.</param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise <see langword="false" />.</returns>
    [Pure]
    [ContractAnnotation("=> true, result: notnull; => false, result: null")]
    public static bool TryParse(
        string? s,
        DateOnly today,
        [MaybeNullWhen(false)] out CoordinationId result)
    {
        return TryParseCore(s, today.Year, out result);
    }

    /// <summary>Span variant of <see cref="Parse(string, DateOnly)"/>.</summary>
    /// <param name="s">The samordningsnummer span to parse.</param>
    /// <param name="today">The reference date for century inference.</param>
    /// <returns>A <see cref="CoordinationId"/> instance.</returns>
    /// <exception cref="InvalidIdNumberException">
    ///     Thrown when <paramref name="s"/> is not a valid samordningsnummer.
    /// </exception>
    [Pure]
    public static CoordinationId Parse(ReadOnlySpan<char> s, DateOnly today)
    {
        var input = s.ToString();
        return TryParseCore(input, today.Year, out var result)
            ? result
            : throw new InvalidIdNumberException(input, InvalidIdNumberReason.InvalidFormat);
    }

    /// <summary>Span variant of <see cref="TryParse(string?, DateOnly, out CoordinationId)"/>.</summary>
    /// <param name="s">The samordningsnummer span to parse.</param>
    /// <param name="today">The reference date for century inference.</param>
    /// <param name="result">The parsed samordningsnummer on success, otherwise null.</param>
    /// <returns><see langword="true"/> if parsing succeeded; otherwise <see langword="false"/>.</returns>
    [Pure]
    [ContractAnnotation("=> true, result: notnull; => false, result: null")]
    public static bool TryParse(
        ReadOnlySpan<char> s,
        DateOnly today,
        [MaybeNullWhen(false)] out CoordinationId result)
        => TryParseCore(s.ToString(), today.Year, out result);

    private static bool TryParseCore(
        string? s,
        int currentYear,
        [MaybeNullWhen(false)] out CoordinationId result)
    {
        result = null;
        var matcher = TryMatch(s);
        if (matcher is null) return false;

        // Coordination constraints: month 1-12 (same as personnummer), encoded day 61-91.
        if (matcher.Month is < 1 or > 12) return false;
        if (matcher.Day is < 61 or > 91) return false;

        var realDay = matcher.Day - 60;

        var century = SwedishIdParsing.ResolveCentury(
            matcher.HasCentury ? matcher.CenturyValue : null,
            matcher.Year,
            currentYear,
            matcher.Delimiter is "+");
        var fullYear = century * 100 + matcher.Year;

        // Validate calendar date using the REAL day (rejects e.g. February 30).
        if (realDay > DateTime.DaysInMonth(fullYear, matcher.Month)) return false;

        // Luhn on the 10-digit form (YYMMDDXXXX) — uses the ENCODED day (the on-the-card form).
        Span<char> tenDigits = stackalloc char[10];
        matcher.YearText.AsSpan().CopyTo(tenDigits[..2]);
        matcher.MonthText.AsSpan().CopyTo(tenDigits[2..4]);
        matcher.DayText.AsSpan().CopyTo(tenDigits[4..6]);
        matcher.Unique.AsSpan().CopyTo(tenDigits[6..10]);
        if (!SwedishLuhnAlgorithm.IsValid(tenDigits)) return false;

        var normalised = $"{fullYear:0000}{matcher.MonthText}{matcher.DayText}{matcher.Unique}";
        result = new CoordinationId(normalised);
        return true;
    }

    /// <summary>Returns true when <paramref name="s" /> is a valid samordningsnummer.</summary>
    /// <param name="s">The input string to validate, or null.</param>
    [Pure]
    public new static bool IsValid(string? s)
    {
        return TryParse(s, out _);
    }

    /// <inheritdoc />
    [Pure]
    public override string LongFormat()
    {
        return _normalised;
    }

    /// <inheritdoc />
    [Pure]
    public override string ShortFormat()
    {
        return _normalised[2..];
    }

    /// <inheritdoc />
    public override string Format(PnrFormat format)
    {
        return FormatCore(format, SwedenClock.Today());
    }

    /// <summary>
    ///     Formats the samordningsnummer using <paramref name="timeProvider" /> to
    ///     determine the centenarian "+" separator (where <paramref name="format" />
    ///     requests the separator-bearing variant).
    /// </summary>
    /// <param name="format">The desired output format.</param>
    /// <param name="timeProvider">
    ///     The time provider used to determine the current UTC instant. The library
    ///     converts the UTC instant to Sweden's civil timezone (<c>Europe/Stockholm</c>);
    ///     the provider's <see cref="TimeProvider.LocalTimeZone" /> is intentionally
    ///     ignored to prevent host-timezone drift on cloud containers.
    /// </param>
    /// <returns>The formatted samordningsnummer string.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="timeProvider" /> is <see langword="null" />.
    /// </exception>
    [Pure]
    public string Format(PnrFormat format, TimeProvider timeProvider)
    {
        return FormatCore(format, SwedenClock.Today(timeProvider));
    }

    /// <summary>
    ///     Formats this samordningsnummer using <paramref name="today" /> as the
    ///     reference date for centenarian-separator inference. Fully
    ///     deterministic — does not read any clock.
    /// </summary>
    /// <param name="format">The desired output format.</param>
    /// <param name="today">
    ///     The reference date used to choose between <c>-</c> and <c>+</c>
    ///     separators in <see cref="PnrFormat.LongFormatWithSeparator" /> and
    ///     <see cref="PnrFormat.ShortFormatWithSeparator" />: <c>+</c> when
    ///     the bearer is 100 or more years old as of <paramref name="today" />,
    ///     <c>-</c> otherwise.
    /// </param>
    /// <returns>The formatted samordningsnummer string.</returns>
    [Pure]
    public string Format(PnrFormat format, DateOnly today)
        => FormatCore(format, today);

    private string FormatCore(PnrFormat format, DateOnly today)
    {
        var inferredSep = InferSeparator(today);
        var year12 = _normalised.AsSpan(0, 4);
        var year10 = _normalised.AsSpan(2, 2);
        var monthDay = _normalised.AsSpan(4, 4);
        var unique = _normalised.AsSpan(8, 4);

        return format switch
        {
            PnrFormat.LongFormat => _normalised,
            PnrFormat.ShortFormat => string.Concat(year10, monthDay, unique),
            PnrFormat.LongFormatWithStandardSeparator => $"{year12}{monthDay}-{unique}",
            PnrFormat.ShortFormatWithStandardSeparator => $"{year10}{monthDay}-{unique}",
            PnrFormat.LongFormatWithSeparator => $"{year12}{monthDay}{inferredSep}{unique}",
            PnrFormat.ShortFormatWithSeparator => $"{year10}{monthDay}{inferredSep}{unique}",
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
    }
}
