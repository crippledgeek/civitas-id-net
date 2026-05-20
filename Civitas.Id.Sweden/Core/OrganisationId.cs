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
///     A Swedish organisation number (organisationsnummer).
///     Always serialised in the canonical 10-digit form on the wire.
/// </summary>
[TypeConverter(typeof(OrganisationIdTypeConverter))]
public sealed record OrganisationId : SwedishOfficialId,
    ISpanParsable<OrganisationId>
{
    /// <summary>
    ///     For Enskild firma (sole-proprietor) orgnummer, the century captured at parse time
    ///     (e.g. 19 or 20). <c>null</c> for legal-person orgnummer or when unknown.
    ///     Used to reconstruct the 12-digit form when delegating to <see cref="PersonalId" /> /
    ///     <see cref="CoordinationId" /> formatting (so '+' centenarian semantics work correctly).
    /// </summary>
    private readonly int? _personCentury;

    /// <summary>The canonical 10-digit form (no separator, no "16" prefix).</summary>
    private readonly string _tenDigits;

    private OrganisationId(string tenDigits, int? personCentury = null)
    {
        _tenDigits = tenDigits;
        _personCentury = personCentury;
    }

    /// <summary>The organisation form derived from the first two (or three) digits.</summary>
    /// <remarks>
    ///     Returns <see cref="OrganisationForm.None" /> for physical-person orgnummer (Enskild firma)
    ///     where the underlying digits are a personnummer or samordningsnummer reused as a
    ///     sole-proprietor identifier. Determined at parse time, not by digit-pattern heuristic.
    ///     For legal-person orgnummer, looks up the form code from the first two digits, falling
    ///     back to <see cref="OrganisationForm.JuridiskFormEjUtredd" /> for unknown codes.
    ///     <para>
    ///         Special case: orgnummer in the sub-ranges <c>556…</c> (pre-2015) and
    ///         <c>559…</c> (post-2015) are <see cref="OrganisationForm.AktiebolagOvriga" />,
    ///         not <see cref="OrganisationForm.EuropakooperativEgtsEric" />. Per Bolagsverket's
    ///         2015-01-12 announcement, the historical <c>556…</c> range was exhausted
    ///         (last <c>556999-9997</c>) and newly issued Aktiebolag use <c>559…</c>
    ///         (first <c>559000-0005</c>). Other <c>55x</c> prefixes (<c>5500</c>–<c>5559</c>)
    ///         remain Europakooperativ / EGTS / ERIC.
    ///     </para>
    /// </remarks>
    public OrganisationForm Form
    {
        get
        {
            // _personCentury is set only when TryParse identified the input as Enskild firma
            // (personnummer or samordningsnummer shape). Legal-person leaves it null.
            if (_personCentury is not null) return OrganisationForm.None;

            // Bolagsverket: orgnummer starting 556xxx (pre-2015) and 559xxx (post-2015) are
            // Aktiebolag despite the first two digits being "55", which would otherwise map to
            // Europakooperativ. See the 2015-01-12 announcement
            // "Aktiebolag får 559 i början av organisationsnumret".
            // The 556 range was exhausted at 556999-9997 in Jan 2015; new Aktiebolag are
            // issued in the 559 range from 559000-0005+. Real-world example: 5560160680 =
            // Telefonaktiebolaget LM Ericsson.
            var threeDigitPrefix = int.Parse(_tenDigits.AsSpan(0, 3), CultureInfo.InvariantCulture);
            if (threeDigitPrefix is 556 or 559)
                return OrganisationForm.AktiebolagOvriga;

            var code = int.Parse(_tenDigits.AsSpan(0, 2), CultureInfo.InvariantCulture);
            return OrganisationFormExtensions.FromCode(code) ?? OrganisationForm.JuridiskFormEjUtredd;
        }
    }

    /// <summary>Whether this number represents a legal or physical person.</summary>
    public OrganisationNumberType NumberType => Form.NumberType;

    // ── IParsable<OrganisationId> ──

    /// <inheritdoc cref="IParsable{TSelf}.Parse(string, IFormatProvider?)" />
    static OrganisationId IParsable<OrganisationId>.Parse(string s, IFormatProvider? provider)
    {
        return Parse(s);
    }

    /// <inheritdoc cref="IParsable{TSelf}.TryParse(string?, IFormatProvider?, out TSelf)" />
    static bool IParsable<OrganisationId>.TryParse(
        string? s, IFormatProvider? provider,
        [MaybeNullWhen(false)] out OrganisationId result)
    {
        return TryParse(s, out result);
    }

    // ── ISpanParsable<OrganisationId> ──

    /// <summary>Parses an orgnummer span. Throws on failure.</summary>
    /// <param name="s">The ID span to parse.</param>
    /// <param name="provider">Format provider — accepted and ignored (Swedish IDs are culture-invariant).</param>
    /// <returns>A valid <see cref="OrganisationId" />.</returns>
    /// <exception cref="InvalidIdNumberException">When <paramref name="s" /> is not a valid organisation number.</exception>
    [Pure]
    public static OrganisationId Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        return TryParse(s, provider, out var result)
            ? result
            : throw new InvalidIdNumberException(s.ToString(), InvalidIdNumberReason.InvalidFormat);
    }

    /// <summary>Attempts to parse an orgnummer span.</summary>
    /// <param name="s">The ID span to parse.</param>
    /// <param name="provider">Format provider — accepted and ignored.</param>
    /// <param name="result">The parsed <see cref="OrganisationId" /> when successful.</param>
    /// <returns>True when parsing succeeds.</returns>
    [Pure]
    [ContractAnnotation("=> true, result: notnull; => false, result: null")]
    public static bool TryParse(
        ReadOnlySpan<char> s, IFormatProvider? provider,
        [MaybeNullWhen(false)] out OrganisationId result)
    {
        return TryParse(s.ToString(), out result);
    }

    /// <summary>
    ///     Internal factory for constructing an <see cref="OrganisationId" /> from a person
    ///     official ID (PersonalId or CoordinationId). Bypasses the month &gt;= 20 validation
    ///     because the caller has already validated the digits as a person ID.
    /// </summary>
    /// <param name="tenDigits">The 10-digit canonical form (no separator, no "16" prefix).</param>
    /// <param name="personCentury">
    ///     The 2-digit century captured at parse time (e.g. 19 or 20). Required for round-tripping
    ///     back to <see cref="PhysicalPersonId" /> with the correct birth year.
    /// </param>
    /// <returns>An OrganisationId wrapping the provided digits.</returns>
    /// <exception cref="ArgumentException">When <paramref name="tenDigits" /> is not exactly 10 characters.</exception>
    internal static OrganisationId FromPhysicalPersonId(string tenDigits, int personCentury)
    {
        ArgumentNullException.ThrowIfNull(tenDigits);
        return tenDigits.Length != 10
            ? throw new ArgumentException("Expected exactly 10 digits.", nameof(tenDigits))
            : new OrganisationId(tenDigits, personCentury);
    }

    /// <summary>
    ///     Internal factory for constructing an <see cref="OrganisationId" /> from
    ///     an already-validated 10-digit normalised string. Skips re-parsing —
    ///     caller MUST guarantee validity (correct length, valid Luhn, valid
    ///     legal-form code).
    /// </summary>
    /// <remarks>
    ///     Used by the <c>Civitas.Id.Sweden.Fakers</c> companion package to
    ///     avoid redundant validation when it just generated the input.
    ///     External callers should use <see cref="Parse(string)" /> instead.
    /// </remarks>
    /// <param name="tenDigitsNormalised">The 10-digit canonical form (no separator, no "16" prefix).</param>
    /// <returns>An OrganisationId wrapping the provided digits.</returns>
    internal static OrganisationId FromValidated(string tenDigitsNormalised)
    {
        Debug.Assert(tenDigitsNormalised is not null);
        Debug.Assert(tenDigitsNormalised.Length == 10);
        return new OrganisationId(tenDigitsNormalised);
    }

    /// <summary>
    ///     Internal factory: constructs an <see cref="OrganisationId"/> from an
    ///     already-validated 10-digit body and the optional person-century
    ///     captured during Enskild firma parsing.
    /// </summary>
    /// <param name="tenDigitsNormalised">A valid 10-digit canonical body.</param>
    /// <param name="personCentury">
    ///     The 2-digit century if the input was parsed as Enskild firma;
    ///     <see langword="null"/> for legal-person organisation numbers.
    /// </param>
    internal static OrganisationId FromValidated(string tenDigitsNormalised, int? personCentury)
    {
        Debug.Assert(tenDigitsNormalised is not null);
        Debug.Assert(tenDigitsNormalised.Length == 10);
        return new OrganisationId(tenDigitsNormalised, personCentury);
    }

    /// <summary>
    ///     Returns this organisation number as a <see cref="PersonalId" /> or <see cref="CoordinationId" />
    ///     when it represents a physical person (sole proprietor). Returns null for legal persons.
    /// </summary>
    /// <returns>The corresponding person ID, or null when this is a legal-person orgnummer.</returns>
    [Pure]
    public PhysicalPersonId? ToPhysicalPersonId()
    {
        if (NumberType != OrganisationNumberType.PhysicalPerson) return null;
        // _personCentury is set iff this OrganisationId was parsed as Enskild firma
        // (NumberType == PhysicalPerson) — these invariants move together at parse time.
        if (_personCentury is not { } c) return null;

        // Compose the canonical 12-digit form. The digits are already validated
        // (Luhn + calendar date) by the OrganisationId.TryParse code path, so we
        // skip re-parsing entirely and dispatch by the day-digit range.
        var normalised = c.ToString("00", CultureInfo.InvariantCulture) + _tenDigits;

        // Day digits live at positions 6-7 of the 12-digit form (CCYYMMDDXXXX).
        // 01-31 → personnummer; 61-91 → samordningsnummer (day +60-offset).
        var dayDigits = (normalised[6] - '0') * 10 + (normalised[7] - '0');
        return dayDigits >= 61
            ? CoordinationId.FromValidated(normalised)
            : PersonalId.FromValidated(normalised);
    }

    /// <summary>Parses an organisation number string. Throws on failure.</summary>
    /// <param name="s">The input string to parse.</param>
    /// <returns>A <see cref="OrganisationId" /> instance.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="s" /> is null.</exception>
    /// <exception cref="InvalidIdNumberException">When <paramref name="s" /> is not a valid organisation number.</exception>
    [Pure]
    public static OrganisationId Parse(string s)
    {
        ArgumentNullException.ThrowIfNull(s);
        return TryParse(s, out var result)
            ? result
            : throw new InvalidIdNumberException(s, InvalidIdNumberReason.InvalidFormat);
    }

    /// <summary>Attempts to parse an organisation number string.</summary>
    /// <param name="s">The input string to parse, or null.</param>
    /// <param name="result">On success, the parsed <see cref="OrganisationId" />; otherwise null.</param>
    /// <returns><c>true</c> when parsing succeeded; otherwise <c>false</c>.</returns>
    [Pure]
    [ContractAnnotation("=> true, result: notnull; => false, result: null")]
    public static bool TryParse(
        string? s,
        [MaybeNullWhen(false)] out OrganisationId result)
    {
        return SwedishIdParsing.TryParseOrganisation(s, out result);
    }

    /// <summary>Returns true when <paramref name="s" /> is a valid organisation number.</summary>
    /// <param name="s">The input string to validate, or null.</param>
    [Pure]
    public new static bool IsValid(string? s)
    {
        return TryParse(s, out _);
    }

    /// <inheritdoc />
    /// <remarks>
    ///     Returns the canonical 10-digit form per Lag (1974:174) §4 — organisationsnummer is legally
    ///     10 digits regardless of underlying type (legal person or Enskild firma). The 12-digit personnummer
    ///     view of an Enskild firma is available via <see cref="ToPhysicalPersonId" />.
    /// </remarks>
    [Pure]
    public override string LongFormat()
    {
        return _tenDigits;
    }

    /// <inheritdoc />
    /// <remarks>Returns the canonical 10-digit form.</remarks>
    [Pure]
    public override string ShortFormat()
    {
        return _tenDigits;
    }

    /// <inheritdoc />
    /// <remarks>
    ///     Per Lag (1974:174) §4, organisationsnummer is 10 digits. ALL six <see cref="PnrFormat" />
    ///     variants produce 10-digit output regardless of underlying type. For 12-digit personnummer
    ///     views of Enskild firma, use <see cref="ToPhysicalPersonId" />.
    ///     The "WithSeparator" variants preserve the input separator (typically '-', or '+' for
    ///     centenarian Enskild firma per Swedish convention). The "WithStandardSeparator" variants
    ///     always use '-'.
    /// </remarks>
    public override string Format(PnrFormat format)
    {
        return FormatCore(format, SwedenClock.Today());
    }

    /// <summary>
    ///     Formats the organisation number using <paramref name="timeProvider" />
    ///     for any time-dependent decisions (e.g. the Enskild firma centenarian
    ///     "+" separator). The provider's <see cref="TimeProvider.LocalTimeZone" />
    ///     is intentionally ignored — the timezone is library-fixed to
    ///     <c>Europe/Stockholm</c>.
    /// </summary>
    /// <param name="format">The desired output format.</param>
    /// <param name="timeProvider">The time provider used to determine the current Stockholm date.</param>
    /// <returns>The formatted organisation number string.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="timeProvider" /> is <see langword="null" />.
    /// </exception>
    [Pure]
    public string Format(PnrFormat format, TimeProvider timeProvider)
    {
        return FormatCore(format, SwedenClock.Today(timeProvider));
    }

    /// <summary>
    ///     Parses an organisationsnummer. The <paramref name="today" /> parameter
    ///     is accepted for API consistency with <see cref="PersonalId.Parse(string, DateOnly)" />
    ///     and <see cref="CoordinationId.Parse(string, DateOnly)" /> so that
    ///     <see cref="SwedishOfficialId.ParseAny(string)" /> dispatchers can pass
    ///     a single reference date uniformly across all three subtypes.
    ///     Organisation-number parsing has no clock dependency: Enskild firma
    ///     requires explicit century in the input; legal-person numbers do not
    ///     infer century at all. The parameter is therefore ignored.
    /// </summary>
    /// <param name="s">The organisationsnummer string to parse.</param>
    /// <param name="today">Ignored; accepted for API consistency.</param>
    /// <returns>An <see cref="OrganisationId" /> instance.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="s" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidIdNumberException">
    ///     Thrown when <paramref name="s" /> is not a valid organisationsnummer.
    /// </exception>
    [Pure]
    public static OrganisationId Parse(string s, DateOnly today)
    {
        _ = today; // intentionally unused — see remarks
        return Parse(s);
    }

    /// <inheritdoc cref="Parse(string, DateOnly)" />
    [Pure]
    public static OrganisationId Parse(ReadOnlySpan<char> s, DateOnly today)
    {
        _ = today;
        return Parse(s.ToString());
    }

    /// <summary>
    ///     Tries to parse an organisationsnummer. The <paramref name="today" />
    ///     parameter is accepted for API consistency; see
    ///     <see cref="Parse(string, DateOnly)" /> remarks. The parameter is ignored.
    /// </summary>
    /// <param name="s">The organisationsnummer string to parse, or null.</param>
    /// <param name="today">Ignored; accepted for API consistency.</param>
    /// <param name="result">The parsed organisationsnummer on success, otherwise null.</param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise <see langword="false" />.</returns>
    [Pure]
    [ContractAnnotation("=> true, result: notnull; => false, result: null")]
    public static bool TryParse(
        string? s,
        DateOnly today,
        [MaybeNullWhen(false)] out OrganisationId result)
    {
        _ = today;
        return TryParse(s, out result);
    }

    /// <inheritdoc cref="TryParse(string?, DateOnly, out OrganisationId)" />
    [Pure]
    [ContractAnnotation("=> true, result: notnull; => false, result: null")]
    public static bool TryParse(
        ReadOnlySpan<char> s,
        DateOnly today,
        [MaybeNullWhen(false)] out OrganisationId result)
    {
        _ = today;
        return TryParse(s.ToString(), out result);
    }

    /// <summary>
    ///     Formats this organisationsnummer using <paramref name="today" /> as the
    ///     reference date for centenarian-separator inference (Enskild firma only).
    ///     Fully deterministic — does not read any clock.
    /// </summary>
    /// <param name="format">The desired output format.</param>
    /// <param name="today">
    ///     The reference date used to choose between <c>-</c> and <c>+</c>
    ///     separators in <see cref="PnrFormat.LongFormatWithSeparator" /> and
    ///     <see cref="PnrFormat.ShortFormatWithSeparator" />: only Enskild firma
    ///     organisationsnummer (with a personnummer or samordningsnummer bearer)
    ///     ever flip to <c>+</c> when the bearer is 100 or more years old as of
    ///     <paramref name="today" />. Legal-person numbers always use <c>-</c>.
    /// </param>
    /// <returns>The formatted organisationsnummer string.</returns>
    [Pure]
    public string Format(PnrFormat format, DateOnly today)
        => FormatCore(format, today);

    private string FormatCore(PnrFormat format, DateOnly today)
    {
        var first6 = _tenDigits.AsSpan(0, 6);
        var last4 = _tenDigits.AsSpan(6, 4);

        // For Enskild firma, the bearer's personnummer/samordningsnummer might have an inferred
        // '+' separator (centenarian convention). Look this up via the underlying person ID.
        // Legal-person orgnummer always use '-'.
        var preservedSeparator = InferOriginalSeparator(today);

        return format switch
        {
            PnrFormat.LongFormat => _tenDigits,
            PnrFormat.ShortFormat => _tenDigits,
            PnrFormat.LongFormatWithStandardSeparator => $"{first6}-{last4}",
            PnrFormat.ShortFormatWithStandardSeparator => $"{first6}-{last4}",
            PnrFormat.LongFormatWithSeparator => $"{first6}{preservedSeparator}{last4}",
            PnrFormat.ShortFormatWithSeparator => $"{first6}{preservedSeparator}{last4}",
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
    }

    /// <summary>
    ///     Returns the separator to use for "WithSeparator" formats:
    ///     '+' for centenarian Enskild firma (per Swedish convention), '-' otherwise.
    /// </summary>
    private string InferOriginalSeparator(DateOnly today)
    {
        if (_personCentury is null) return "-"; // Legal-person — always '-'

        // Enskild firma — derive separator from underlying person's age.
        return ToPhysicalPersonId() is { } p && p.GetAge(today) >= 100 ? "+" : "-";
    }

    /// <summary>
    ///     Returns the canonical 10-digit form. Matches
    ///     <see cref="LongFormat"/>. Round-trippable through
    ///     <see cref="Parse(string)"/>.
    /// </summary>
    /// <returns>The canonical 10-digit string.</returns>
    public override string ToString() => _tenDigits;
}
