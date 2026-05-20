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
///     A Swedish personal identification number (personnummer).
/// </summary>
[TypeConverter(typeof(PersonalIdTypeConverter))]
public sealed record PersonalId : PhysicalPersonId,
    ISwedishPersonIdHooks<PersonalId>,
    ISpanParsable<PersonalId>
{
    private PersonalId(string normalised) : base(normalised)
    {
    }

    // ─── Explicit-interface static-abstract implementations of ISwedishPersonIdHooks<PersonalId>.
    //     Explicit form keeps these off the public API surface; accessible only through
    //     the generic CRTP constraint on PhysicalPersonId.TryParseCore<TSelf>. ───

    static bool ISwedishPersonIdHooks<PersonalId>.IsDayValid(int encodedDay)
        => encodedDay is >= 1 and <= 31;

    static int ISwedishPersonIdHooks<PersonalId>.CalendarDay(int encodedDay)
        => encodedDay;

    static PersonalId ISwedishPersonIdHooks<PersonalId>.FromValidated(string normalised12)
        => new(normalised12);

    /// <summary>The date of birth encoded in this personnummer.</summary>
    public override DateOnly BirthDate
    {
        get
        {
            var year = int.Parse(_normalised.AsSpan(0, 4), CultureInfo.InvariantCulture);
            var month = int.Parse(_normalised.AsSpan(4, 2), CultureInfo.InvariantCulture);
            var day = int.Parse(_normalised.AsSpan(6, 2), CultureInfo.InvariantCulture);
            return new DateOnly(year, month, day);
        }
    }

    /// <inheritdoc />
    protected override char GenderDigit => _normalised[10];

    // ── IParsable<PersonalId> contract ──
    // The provider parameter is accepted and ignored (Swedish IDs are culture-invariant).

    /// <inheritdoc cref="IParsable{TSelf}.Parse(string, IFormatProvider?)" />
    static PersonalId IParsable<PersonalId>.Parse(string s, IFormatProvider? provider)
    {
        return Parse(s);
    }

    /// <inheritdoc cref="IParsable{TSelf}.TryParse(string?, IFormatProvider?, out TSelf)" />
    static bool IParsable<PersonalId>.TryParse(
        string? s, IFormatProvider? provider,
        [MaybeNullWhen(false)] out PersonalId result)
    {
        return TryParse(s, out result);
    }

    // ── ISpanParsable<PersonalId> contract ──

    /// <summary>Parses a personnummer span. Throws on failure.</summary>
    /// <param name="s">The ID span to parse.</param>
    /// <param name="provider">Format provider — accepted and ignored (Swedish IDs are culture-invariant).</param>
    /// <returns>A valid <see cref="PersonalId" />.</returns>
    /// <exception cref="InvalidIdNumberException">When <paramref name="s" /> is not a valid personnummer.</exception>
    [Pure]
    public static PersonalId Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        return TryParse(s, provider, out var result)
            ? result
            : throw new InvalidIdNumberException(s.ToString(), InvalidIdNumberReason.InvalidFormat);
    }

    /// <summary>Attempts to parse a personnummer span.</summary>
    /// <param name="s">The ID span to parse.</param>
    /// <param name="provider">Format provider — accepted and ignored.</param>
    /// <param name="result">The parsed <see cref="PersonalId" /> when successful.</param>
    /// <returns>True when parsing succeeds.</returns>
    [Pure]
    [ContractAnnotation("=> true, result: notnull; => false, result: null")]
    public static bool TryParse(
        ReadOnlySpan<char> s, IFormatProvider? provider,
        [MaybeNullWhen(false)] out PersonalId result)
    {
        return TryParse(s.ToString(), out result);
    }

    /// <summary>
    ///     Internal factory: constructs a <see cref="PersonalId" /> from an already-validated
    ///     12-digit normalised string. Skips re-parsing — caller MUST guarantee validity
    ///     (correct length, valid Luhn, valid calendar date).
    /// </summary>
    /// <param name="normalised12">A canonical 12-digit normalised personnummer (YYYYMMDDXXXX).</param>
    /// <returns>A <see cref="PersonalId" /> wrapping the provided digits.</returns>
    internal static PersonalId FromValidated(string normalised12)
    {
        Debug.Assert(normalised12.Length == 12);
        return new PersonalId(normalised12);
    }

    /// <summary>
    ///     Returns this personnummer viewed as a sole-proprietor organisation number.
    /// </summary>
    /// <returns>An <see cref="OrganisationId" /> with <see cref="OrganisationNumberType.PhysicalPerson" />.</returns>
    [Pure]
    public OrganisationId ToOrganisationId()
    {
        return OrganisationId.FromPhysicalPersonId(
            ShortFormat(),
            int.Parse(_normalised.AsSpan(0, 2), CultureInfo.InvariantCulture));
    }

    /// <summary>Parses a personnummer string. Throws <see cref="InvalidIdNumberException" /> on failure.</summary>
    /// <param name="s">The input string to parse.</param>
    /// <returns>A <see cref="PersonalId" /> instance.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="s" /> is null.</exception>
    /// <exception cref="InvalidIdNumberException">When <paramref name="s" /> is not a valid personnummer.</exception>
    [Pure]
    public static PersonalId Parse(string s)
    {
        ArgumentNullException.ThrowIfNull(s);
        return TryParse(s, out var result)
            ? result
            : throw new InvalidIdNumberException(s, InvalidIdNumberReason.InvalidFormat);
    }

    /// <summary>Attempts to parse a personnummer string.</summary>
    /// <param name="s">The input string to parse, or null.</param>
    /// <param name="result">On success, the parsed <see cref="PersonalId" />; otherwise null.</param>
    /// <returns><c>true</c> when parsing succeeded; otherwise <c>false</c>.</returns>
    [Pure]
    [ContractAnnotation("=> true, result: notnull; => false, result: null")]
    public static bool TryParse(
        string? s,
        [MaybeNullWhen(false)] out PersonalId result)
    {
        return TryParseCore<PersonalId>(s, SwedenClock.Today().Year, out result);
    }

    /// <summary>
    ///     Parses a personnummer using <paramref name="timeProvider" /> for century
    ///     inference of two-digit-year inputs.
    /// </summary>
    /// <param name="s">The personnummer string to parse.</param>
    /// <param name="timeProvider">
    ///     The time provider used to determine the current UTC instant. The library
    ///     converts the UTC instant to Sweden's civil timezone (<c>Europe/Stockholm</c>)
    ///     to derive the current year for century inference; the provider's
    ///     <see cref="TimeProvider.LocalTimeZone" /> is intentionally ignored to
    ///     prevent host-timezone drift on cloud containers.
    /// </param>
    /// <returns>A <see cref="PersonalId" /> instance.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="s" /> or <paramref name="timeProvider" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidIdNumberException">
    ///     Thrown when <paramref name="s" /> is not a valid personnummer.
    /// </exception>
    [Pure]
    public static PersonalId Parse(string s, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(s);
        var currentYear = SwedenClock.Today(timeProvider).Year;
        return TryParseCore<PersonalId>(s, currentYear, out var result)
            ? result
            : throw new InvalidIdNumberException(s, InvalidIdNumberReason.InvalidFormat);
    }

    /// <summary>
    ///     Tries to parse a personnummer using <paramref name="timeProvider" /> for
    ///     century inference of two-digit-year inputs.
    /// </summary>
    /// <param name="s">The personnummer string to parse, or null.</param>
    /// <param name="timeProvider">
    ///     The time provider used to determine the current UTC instant. The library
    ///     converts the UTC instant to Sweden's civil timezone (<c>Europe/Stockholm</c>)
    ///     to derive the current year for century inference; the provider's
    ///     <see cref="TimeProvider.LocalTimeZone" /> is intentionally ignored to
    ///     prevent host-timezone drift on cloud containers.
    /// </param>
    /// <param name="result">The parsed personnummer on success, otherwise null.</param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="timeProvider" /> is <see langword="null" />.
    /// </exception>
    [Pure]
    [ContractAnnotation("=> true, result: notnull; => false, result: null")]
    public static bool TryParse(
        string? s,
        TimeProvider timeProvider,
        [MaybeNullWhen(false)] out PersonalId result)
    {
        var currentYear = SwedenClock.Today(timeProvider).Year;
        return TryParseCore<PersonalId>(s, currentYear, out result);
    }

    /// <summary>
    ///     Parses a personnummer using <paramref name="today" /> as the reference
    ///     date for two-digit-year century inference. Fully deterministic —
    ///     does not read any clock.
    /// </summary>
    /// <param name="s">The personnummer string to parse.</param>
    /// <param name="today">
    ///     The reference date for century inference. Use this overload when you
    ///     have an explicit "as-of" date (data migrations, batch processing,
    ///     deterministic test fixtures). For live applications that read the
    ///     wall clock, prefer <see cref="Parse(string, TimeProvider)" />.
    /// </param>
    /// <returns>A <see cref="PersonalId" /> instance.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="s" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidIdNumberException">
    ///     Thrown when <paramref name="s" /> is not a valid personnummer.
    /// </exception>
    [Pure]
    public static PersonalId Parse(string s, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(s);
        return TryParseCore<PersonalId>(s, today.Year, out var result)
            ? result
            : throw new InvalidIdNumberException(s, InvalidIdNumberReason.InvalidFormat);
    }

    /// <summary>
    ///     Tries to parse a personnummer using <paramref name="today" /> as the
    ///     reference date for two-digit-year century inference. Fully
    ///     deterministic — does not read any clock.
    /// </summary>
    /// <param name="s">The personnummer string to parse, or null.</param>
    /// <param name="today">The reference date for century inference.</param>
    /// <param name="result">The parsed personnummer on success, otherwise null.</param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise <see langword="false" />.</returns>
    [Pure]
    [ContractAnnotation("=> true, result: notnull; => false, result: null")]
    public static bool TryParse(
        string? s,
        DateOnly today,
        [MaybeNullWhen(false)] out PersonalId result)
    {
        return TryParseCore<PersonalId>(s, today.Year, out result);
    }

    /// <summary>Span variant of <see cref="Parse(string, DateOnly)"/>.</summary>
    /// <param name="s">The personnummer span to parse.</param>
    /// <param name="today">The reference date for century inference.</param>
    /// <returns>A <see cref="PersonalId"/> instance.</returns>
    /// <exception cref="InvalidIdNumberException">
    ///     Thrown when <paramref name="s"/> is not a valid personnummer.
    /// </exception>
    [Pure]
    public static PersonalId Parse(ReadOnlySpan<char> s, DateOnly today)
    {
        var input = s.ToString();
        return TryParseCore<PersonalId>(input, today.Year, out var result)
            ? result
            : throw new InvalidIdNumberException(input, InvalidIdNumberReason.InvalidFormat);
    }

    /// <summary>Span variant of <see cref="TryParse(string?, DateOnly, out PersonalId)"/>.</summary>
    /// <param name="s">The personnummer span to parse.</param>
    /// <param name="today">The reference date for century inference.</param>
    /// <param name="result">The parsed personnummer on success, otherwise null.</param>
    /// <returns><see langword="true"/> if parsing succeeded; otherwise <see langword="false"/>.</returns>
    [Pure]
    [ContractAnnotation("=> true, result: notnull; => false, result: null")]
    public static bool TryParse(
        ReadOnlySpan<char> s,
        DateOnly today,
        [MaybeNullWhen(false)] out PersonalId result)
        => TryParseCore<PersonalId>(s.ToString(), today.Year, out result);

    /// <summary>Returns true when <paramref name="s" /> is a valid personnummer.</summary>
    /// <param name="s">The input string to validate, or null.</param>
    [Pure]
    public new static bool IsValid(string? s)
    {
        return TryParse(s, out _);
    }

    /// <inheritdoc cref="PhysicalPersonId.ToString" />
    public sealed override string ToString() => LongFormat();
}
