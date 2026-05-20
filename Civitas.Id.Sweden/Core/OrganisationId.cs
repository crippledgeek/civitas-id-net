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
    ISpanParsable<OrganisationId>,
    ISpanFormattable,
    IUtf8SpanFormattable,
    IUtf8SpanParsable<OrganisationId>
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
        // The two "bare" formats are the canonical 10-digit form — no allocation.
        if (format is PnrFormat.LongFormat or PnrFormat.ShortFormat) return _tenDigits;

        var length = FormattedLength(format);
        return string.Create(
            length,
            (self: this, format, today),
            static (buffer, state) =>
            {
                var ok = state.self.TryFormatCore(buffer, state.format, state.today, out _);
                Debug.Assert(ok);
            });
    }

    /// <summary>
    ///     Returns a culture-invariant <see cref="string"/> representation using
    ///     the given format specifier. Recognised values: <c>null</c> / <c>""</c> /
    ///     <c>"L"</c> = LongFormat (10 digits); <c>"S"</c> = ShortFormat (10
    ///     digits); <c>"LD"</c>/<c>"SD"</c> = with '-' separator;
    ///     <c>"LI"</c>/<c>"SI"</c> = with inferred '+'/'-' separator.
    /// </summary>
    /// <param name="format">The format string (see summary).</param>
    /// <param name="formatProvider">Accepted and ignored — Swedish IDs are culture-invariant.</param>
    /// <returns>The formatted organisation number.</returns>
    /// <exception cref="FormatException">When <paramref name="format"/> is not recognised.</exception>
    [Pure]
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        return Format(ParseFormatSpec(format.AsSpan()));
    }

    /// <summary>
    ///     Tries to format the organisation number into <paramref name="destination"/>
    ///     as a span of characters. Zero allocation when <paramref name="destination"/>
    ///     is large enough.
    /// </summary>
    /// <param name="destination">The destination buffer to write to.</param>
    /// <param name="charsWritten">The number of characters written on success.</param>
    /// <param name="format">The format specifier. See <see cref="ToString(string?, IFormatProvider?)"/>.</param>
    /// <param name="provider">Accepted and ignored — Swedish IDs are culture-invariant.</param>
    /// <returns>
    ///     <see langword="true"/> if the formatted value fit in
    ///     <paramref name="destination"/>; otherwise <see langword="false"/>.
    /// </returns>
    public bool TryFormat(
        Span<char> destination,
        out int charsWritten,
        ReadOnlySpan<char> format,
        IFormatProvider? provider)
    {
        var pnrFormat = ParseFormatSpec(format);
        return TryFormatCore(destination, pnrFormat, SwedenClock.Today(), out charsWritten);
    }

    /// <summary>
    ///     Writes the formatted organisation number into <paramref name="destination"/>
    ///     using <paramref name="today"/> as the reference date for centenarian
    ///     separator inference. Deterministic — does not read any clock.
    /// </summary>
    /// <param name="destination">The destination buffer to write to.</param>
    /// <param name="format">The desired output format.</param>
    /// <param name="today">The reference date used for centenarian '+' inference.</param>
    /// <param name="charsWritten">The number of characters written on success.</param>
    /// <returns>
    ///     <see langword="true"/> if the formatted value fit in
    ///     <paramref name="destination"/>; otherwise <see langword="false"/>.
    /// </returns>
    public bool TryFormat(
        Span<char> destination,
        PnrFormat format,
        DateOnly today,
        out int charsWritten)
    {
        return TryFormatCore(destination, format, today, out charsWritten);
    }

    /// <summary>
    ///     Maps a textual format specifier to <see cref="PnrFormat"/>. Throws
    ///     <see cref="FormatException"/> for unrecognised specifiers.
    /// </summary>
    private static PnrFormat ParseFormatSpec(ReadOnlySpan<char> format)
    {
        return format.Length switch
        {
            0 => PnrFormat.LongFormat,
            1 => format[0] switch
            {
                'L' or 'G' or 'g' or 'l' => PnrFormat.LongFormat,
                'S' or 's' => PnrFormat.ShortFormat,
                _ => throw UnknownFormatSpec(format)
            },
            2 => format[0] switch
            {
                'L' or 'l' => format[1] switch
                {
                    'D' or 'd' => PnrFormat.LongFormatWithStandardSeparator,
                    'I' or 'i' => PnrFormat.LongFormatWithSeparator,
                    _ => throw UnknownFormatSpec(format)
                },
                'S' or 's' => format[1] switch
                {
                    'D' or 'd' => PnrFormat.ShortFormatWithStandardSeparator,
                    'I' or 'i' => PnrFormat.ShortFormatWithSeparator,
                    _ => throw UnknownFormatSpec(format)
                },
                _ => throw UnknownFormatSpec(format)
            },
            _ => throw UnknownFormatSpec(format)
        };
    }

    private static FormatException UnknownFormatSpec(ReadOnlySpan<char> format)
        => new($"Unrecognised format specifier '{format.ToString()}'. " +
               "Use \"L\"/\"S\"/\"LD\"/\"SD\"/\"LI\"/\"SI\" or null.");

    /// <summary>Computes the exact output length for a given <see cref="PnrFormat"/>.</summary>
    private static int FormattedLength(PnrFormat format) => format switch
    {
        PnrFormat.LongFormat => 10,
        PnrFormat.ShortFormat => 10,
        PnrFormat.LongFormatWithSeparator => 11,
        PnrFormat.LongFormatWithStandardSeparator => 11,
        PnrFormat.ShortFormatWithSeparator => 11,
        PnrFormat.ShortFormatWithStandardSeparator => 11,
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
    };

    /// <summary>
    ///     Writes the formatted organisation number into <paramref name="destination"/>
    ///     without allocation. Returns <see langword="false"/> + zeroed
    ///     <paramref name="charsWritten"/> if the destination is too small.
    /// </summary>
    private bool TryFormatCore(
        Span<char> destination,
        PnrFormat format,
        DateOnly today,
        out int charsWritten)
    {
        var required = FormattedLength(format);
        if (destination.Length < required)
        {
            charsWritten = 0;
            return false;
        }

        var src = _tenDigits.AsSpan();
        switch (format)
        {
            case PnrFormat.LongFormat:
            case PnrFormat.ShortFormat:
                src.CopyTo(destination);
                break;
            case PnrFormat.LongFormatWithStandardSeparator:
            case PnrFormat.ShortFormatWithStandardSeparator:
                src[..6].CopyTo(destination);
                destination[6] = '-';
                src[6..10].CopyTo(destination[7..]);
                break;
            case PnrFormat.LongFormatWithSeparator:
            case PnrFormat.ShortFormatWithSeparator:
                src[..6].CopyTo(destination);
                destination[6] = ComputeInferredSeparator(today);
                src[6..10].CopyTo(destination[7..]);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, null);
        }

        charsWritten = required;
        return true;
    }

    /// <summary>
    ///     Returns '+' for Enskild firma whose bearer is 100 or older on
    ///     <paramref name="today"/>; '-' otherwise (including all legal-person
    ///     organisation numbers).
    /// </summary>
    private char ComputeInferredSeparator(DateOnly today)
    {
        if (_personCentury is null) return '-';
        return ToPhysicalPersonId() is { } p && p.GetAge(today) >= 100 ? '+' : '-';
    }

    /// <summary>
    ///     Returns the canonical 10-digit form. Matches
    ///     <see cref="LongFormat"/>. Round-trippable through
    ///     <see cref="Parse(string)"/>.
    /// </summary>
    /// <returns>The canonical 10-digit string.</returns>
    public override string ToString() => _tenDigits;

    // ── IUtf8SpanFormattable ──

    /// <summary>
    ///     Tries to format the organisation number into <paramref name="utf8Destination"/>
    ///     as a span of UTF-8 bytes. Swedish IDs are pure ASCII, so encoding is
    ///     one byte per character; zero allocation when the destination is large
    ///     enough.
    /// </summary>
    /// <param name="utf8Destination">The destination byte buffer.</param>
    /// <param name="bytesWritten">The number of bytes written on success.</param>
    /// <param name="format">The format specifier. See <see cref="ToString(string?, IFormatProvider?)"/>.</param>
    /// <param name="provider">Accepted and ignored — Swedish IDs are culture-invariant.</param>
    /// <returns>
    ///     <see langword="true"/> if the formatted value fit; otherwise
    ///     <see langword="false"/>.
    /// </returns>
    public bool TryFormat(
        Span<byte> utf8Destination,
        out int bytesWritten,
        ReadOnlySpan<char> format,
        IFormatProvider? provider)
    {
        var pnrFormat = ParseFormatSpec(format);
        return TryFormatUtf8Core(utf8Destination, pnrFormat, SwedenClock.Today(), out bytesWritten);
    }

    /// <summary>
    ///     Writes the formatted organisation number into <paramref name="utf8Destination"/>
    ///     as UTF-8 bytes using <paramref name="today"/> as the reference date for
    ///     centenarian separator inference. Deterministic — does not read any clock.
    /// </summary>
    /// <param name="utf8Destination">The destination byte buffer.</param>
    /// <param name="format">The desired output format.</param>
    /// <param name="today">The reference date used for the centenarian '+' separator.</param>
    /// <param name="bytesWritten">The number of bytes written on success.</param>
    /// <returns>
    ///     <see langword="true"/> if the formatted value fit; otherwise <see langword="false"/>.
    /// </returns>
    public bool TryFormat(
        Span<byte> utf8Destination,
        PnrFormat format,
        DateOnly today,
        out int bytesWritten)
    {
        return TryFormatUtf8Core(utf8Destination, format, today, out bytesWritten);
    }

    private bool TryFormatUtf8Core(
        Span<byte> utf8Destination,
        PnrFormat format,
        DateOnly today,
        out int bytesWritten)
    {
        var required = FormattedLength(format);
        if (utf8Destination.Length < required)
        {
            bytesWritten = 0;
            return false;
        }

        var src = _tenDigits.AsSpan();
        switch (format)
        {
            case PnrFormat.LongFormat:
            case PnrFormat.ShortFormat:
                AsciiCharsToBytes(src, utf8Destination);
                break;
            case PnrFormat.LongFormatWithStandardSeparator:
            case PnrFormat.ShortFormatWithStandardSeparator:
                AsciiCharsToBytes(src[..6], utf8Destination);
                utf8Destination[6] = (byte)'-';
                AsciiCharsToBytes(src[6..10], utf8Destination[7..]);
                break;
            case PnrFormat.LongFormatWithSeparator:
            case PnrFormat.ShortFormatWithSeparator:
                AsciiCharsToBytes(src[..6], utf8Destination);
                utf8Destination[6] = (byte)ComputeInferredSeparator(today);
                AsciiCharsToBytes(src[6..10], utf8Destination[7..]);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, null);
        }

        bytesWritten = required;
        return true;
    }

    private static void AsciiCharsToBytes(ReadOnlySpan<char> source, Span<byte> destination)
    {
        for (var i = 0; i < source.Length; i++)
        {
            destination[i] = (byte)source[i];
        }
    }

    // ── IUtf8SpanParsable<OrganisationId> ──

    /// <summary>Parses an organisation number from a UTF-8 byte span. Throws on failure.</summary>
    /// <param name="s">The UTF-8 source span.</param>
    /// <param name="provider">Format provider — accepted and ignored.</param>
    /// <returns>A valid <see cref="OrganisationId"/>.</returns>
    /// <exception cref="InvalidIdNumberException">When parsing fails.</exception>
    [Pure]
    public static OrganisationId Parse(ReadOnlySpan<byte> s, IFormatProvider? provider)
    {
        return TryParse(s, provider, out var result)
            ? result
            : throw new InvalidIdNumberException(
                System.Text.Encoding.UTF8.GetString(s),
                InvalidIdNumberReason.InvalidFormat);
    }

    /// <summary>Attempts to parse an organisation number from a UTF-8 byte span.</summary>
    /// <param name="s">The UTF-8 source span.</param>
    /// <param name="provider">Format provider — accepted and ignored.</param>
    /// <param name="result">The parsed value on success.</param>
    /// <returns><see langword="true"/> on success.</returns>
    [Pure]
    [ContractAnnotation("=> true, result: notnull; => false, result: null")]
    public static bool TryParse(
        ReadOnlySpan<byte> s,
        IFormatProvider? provider,
        [MaybeNullWhen(false)] out OrganisationId result)
    {
        result = null;
        if (s.Length is 0 or > 100) return false;
        Span<char> charBuffer = stackalloc char[s.Length];
        for (var i = 0; i < s.Length; i++)
        {
            var b = s[i];
            if (b > 127) return false;
            charBuffer[i] = (char)b;
        }
        var input = new string(charBuffer);
        return TryParse(input, out result);
    }
}
