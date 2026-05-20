using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Civitas.Id.Sweden.Errors;
using Civitas.Id.Sweden.Format;
using Civitas.Id.Sweden.Internal;
using JetBrains.Annotations;

namespace Civitas.Id.Sweden.Core;

/// <summary>
///     Common base for Swedish official ID numbers that encode a person's date of birth —
///     <see cref="PersonalId" /> (personnummer) and <see cref="CoordinationId" />
///     (samordningsnummer).
/// </summary>
/// <remarks>
///     <para>
///         This intermediate abstract record exists because <see cref="OrganisationId" />
///         shares the parsing/formatting surface of <see cref="SwedishOfficialId" /> but
///         does not carry a date of birth. Hoisting the age-related members to a
///         person-specific base preserves the sum-type sealed hierarchy while letting
///         <see cref="PersonalId" /> and <see cref="CoordinationId" /> share their
///         identical age computation logic via Template Method.
///     </para>
///     <para>
///         JIT devirtualisation handles the abstract <see cref="BirthDate" /> hook
///         without measurable overhead for sealed records on .NET 10; NativeAOT
///         devirtualises via Class Hierarchy Analysis. Compatible with
///         <c>IsAotCompatible=true</c>.
///     </para>
/// </remarks>
public abstract record PhysicalPersonId : SwedishOfficialId, ISpanFormattable, IUtf8SpanFormattable
{
    /// <summary>Canonical 12-digit normalised form (YYYYMMDDXXXX).</summary>
    /// <remarks>
    ///     Non-positional <see langword="private protected"/> field. By design it
    ///     does NOT appear in synthesised <see cref="object.ToString()"/> output
    ///     (records' <c>PrintMembers</c> emits public members only). The overridden
    ///     <see cref="ToString()"/> returns this value directly per the
    ///     Vogen/StronglyTypedId industry pattern for value-object records.
    /// </remarks>
    // ReSharper disable once InconsistentNaming -- project convention: _camelCase for backing fields
    // including private protected (matches the pre-lift field names on PersonalId/CoordinationId/OrganisationId).
    private protected readonly string _normalised;

    /// <summary>Initialises the canonical normalised form.</summary>
    /// <param name="normalised12">A valid 12-digit YYYYMMDDXXXX body.</param>
    private protected PhysicalPersonId(string normalised12)
    {
        Debug.Assert(normalised12 is not null);
        Debug.Assert(normalised12.Length == 12);
        _normalised = normalised12;
    }

    /// <summary>The decoded date of birth for this ID.</summary>
    /// <remarks>
    ///     Kept abstract on this base because the day-decoding rule differs
    ///     between subtypes: <see cref="PersonalId"/> reads the day digits
    ///     literally, while <see cref="CoordinationId"/> subtracts 60 (the
    ///     samordningsnummer day-offset convention).
    /// </remarks>
    public abstract DateOnly BirthDate { get; }

    /// <summary>The gender-encoding digit from the normalised ID (digit 11 of the 12-digit form).</summary>
    /// <remarks>
    ///     In Swedish personnummer / samordningsnummer, the 11th digit is odd for male, even for female,
    ///     per Skatteverket's specification. This abstract hook is the per-variant accessor; concrete
    ///     subtypes provide the digit by reading their normalised representation.
    /// </remarks>
    protected abstract char GenderDigit { get; }

    /// <summary>True if the gender digit (position 11 of the 12-digit form) is odd — male.</summary>
    public bool IsMale => (GenderDigit - '0') % 2 == 1;

    /// <summary>True if the gender digit is even — female.</summary>
    public bool IsFemale => !IsMale;

    /// <summary>
    ///     Returns the age in full years on the given date.
    /// </summary>
    /// <param name="today">The reference date for age calculation.</param>
    /// <returns>The age in years (years since <see cref="BirthDate" />, not crossing the next birthday).</returns>
    [Pure]
    [NonNegativeValue]
    public int GetAge(DateOnly today)
    {
        var age = today.Year - BirthDate.Year;
        if (today < BirthDate.AddYears(age)) age--;
        return age;
    }

    /// <summary>
    ///     Returns the person's age in whole years as of the current calendar date
    ///     in Sweden's civil timezone (<c>Europe/Stockholm</c>), regardless of the
    ///     host process's local timezone.
    /// </summary>
    /// <remarks>
    ///     For deterministic results, pass an explicit <see cref="DateOnly" />
    ///     via <see cref="GetAge(DateOnly)" /> or a <see cref="TimeProvider" /> via
    ///     <see cref="GetAge(TimeProvider)" />. The Stockholm pin reflects
    ///     Föräldrabalken 9 kap 1 § + Lag (1930:173) §1 + Förordning (1979:988) /
    ///     Förordning (2001:127); see project documentation for legal-compliance
    ///     citations.
    /// </remarks>
    [NonNegativeValue]
    public int GetAge()
    {
        return GetAge(SwedenClock.Today());
    }

    /// <summary>
    ///     Returns the person's age in whole years as of the current calendar date
    ///     in Sweden's civil timezone (<c>Europe/Stockholm</c>), using
    ///     <paramref name="timeProvider" /> only to obtain the current UTC instant.
    /// </summary>
    /// <param name="timeProvider">
    ///     The time provider used to determine the current UTC instant. The library
    ///     converts the UTC instant to Sweden's civil timezone (<c>Europe/Stockholm</c>);
    ///     the provider's <see cref="TimeProvider.LocalTimeZone" /> is intentionally
    ///     ignored to prevent host-timezone drift on cloud containers.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="timeProvider" /> is <see langword="null" />.
    /// </exception>
    [Pure]
    [NonNegativeValue]
    public int GetAge(TimeProvider timeProvider)
    {
        return GetAge(SwedenClock.Today(timeProvider));
    }

    /// <summary>True if the person is 18 or older on the given date.</summary>
    /// <param name="today">The reference date for the age check.</param>
    [Pure]
    public bool IsAdult(DateOnly today)
    {
        return GetAge(today) >= 18;
    }

    /// <summary>
    ///     True if the person is 18 or older as of the current calendar date in
    ///     Sweden's civil timezone (<c>Europe/Stockholm</c>), regardless of the
    ///     host process's local timezone. For deterministic results, use
    ///     <see cref="IsAdult(DateOnly)" /> or <see cref="IsAdult(TimeProvider)" />.
    /// </summary>
    public bool IsAdult()
    {
        return IsAdult(SwedenClock.Today());
    }

    /// <summary>
    ///     Returns <see langword="true" /> if the person is 18 or older as of the
    ///     current calendar date in Sweden's civil timezone (<c>Europe/Stockholm</c>).
    /// </summary>
    /// <param name="timeProvider">
    ///     The time provider used to determine the current UTC instant. The library
    ///     converts the UTC instant to Sweden's civil timezone (<c>Europe/Stockholm</c>);
    ///     the provider's <see cref="TimeProvider.LocalTimeZone" /> is intentionally
    ///     ignored to prevent host-timezone drift on cloud containers.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="timeProvider" /> is <see langword="null" />.
    /// </exception>
    [Pure]
    public bool IsAdult(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        return GetAge(timeProvider) >= 18;
    }

    /// <summary>True if the person is younger than 18 on the given date.</summary>
    /// <param name="today">The reference date for the age check.</param>
    [Pure]
    public bool IsChild(DateOnly today)
    {
        return !IsAdult(today);
    }

    /// <summary>
    ///     True if the person is younger than 18 as of the current calendar date
    ///     in Sweden's civil timezone (<c>Europe/Stockholm</c>), regardless of the
    ///     host process's local timezone. For deterministic results, use
    ///     <see cref="IsChild(DateOnly)" /> or <see cref="IsChild(TimeProvider)" />.
    /// </summary>
    public bool IsChild()
    {
        return !IsAdult();
    }

    /// <summary>
    ///     Returns <see langword="true" /> if the person is under 18 as of the
    ///     current calendar date in Sweden's civil timezone (<c>Europe/Stockholm</c>).
    /// </summary>
    /// <param name="timeProvider">
    ///     The time provider used to determine the current UTC instant. The library
    ///     converts the UTC instant to Sweden's civil timezone (<c>Europe/Stockholm</c>);
    ///     the provider's <see cref="TimeProvider.LocalTimeZone" /> is intentionally
    ///     ignored to prevent host-timezone drift on cloud containers.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="timeProvider" /> is <see langword="null" />.
    /// </exception>
    [Pure]
    public bool IsChild(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        return !IsAdult(timeProvider);
    }

    /// <summary>
    ///     Shared parse dispatcher for <see cref="PersonalId"/> and
    ///     <see cref="CoordinationId"/>. Hooks dispatched via
    ///     <see cref="ISwedishPersonIdHooks{TSelf}"/> static abstract members.
    /// </summary>
    /// <remarks>
    ///     Returns <see langword="bool"/> per the <see cref="IParsable{TSelf}"/>
    ///     convention. Granular failure reason (date vs Luhn vs format) is
    ///     deliberately not exposed through this dispatcher; the throwing
    ///     <c>Parse</c> entry-points surface <see cref="InvalidIdNumberReason"/>
    ///     via <see cref="InvalidIdNumberException"/>. A <c>TryParseWithReason</c>
    ///     overload can be added non-breakingly if a caller needs reason-on-failure
    ///     semantics.
    /// </remarks>
    [Pure]
    [ContractAnnotation("=> true, result: notnull; => false, result: null")]
    private protected static bool TryParseCore<TSelf>(
        string? s, int currentYear,
        [MaybeNullWhen(false)] out TSelf result)
        where TSelf : PhysicalPersonId, ISwedishPersonIdHooks<TSelf>
    {
        result = null;
        if (s is null) return false;
        if (!SwedishIdParsing.TryMatchSpan(s.AsSpan(), out var match)) return false;

        if (match.Month is < 1 or > 12) return false;
        if (!TSelf.IsDayValid(match.Day)) return false;

        var century = SwedishIdParsing.ResolveCentury(
            match.HasCentury ? match.CenturyValue : null,
            match.Year,
            currentYear,
            match.Delimiter == '+');
        var fullYear = century * 100 + match.Year;
        var realDay = TSelf.CalendarDay(match.Day);

        if (!SwedishIdParsing.TryValidatePersonShapedBody(in match, fullYear, realDay))
            return false;

        // Fast-path: input is already the canonical 12-digit form (YYYYMMDDXXXX),
        // no SE prefix, no delimiter, no whitespace. Reuse the input string rather
        // than allocating a new copy. The match.Source == trimmed body — if its
        // length equals the input's length, no SE prefix was stripped and no
        // whitespace was trimmed. Combined with HasCentury, no delimiter, and
        // length 12, the input IS the canonical form bit-for-bit.
        string normalised;
        if (s.Length == 12 && match.HasCentury && match.Delimiter == '\0' && match.Source.Length == s.Length)
        {
            normalised = s;
        }
        else
        {
            Span<char> normalisedSpan = stackalloc char[12];
            fullYear.TryFormat(normalisedSpan[..4], out _, "D4", CultureInfo.InvariantCulture);
            match.MonthTextSpan.CopyTo(normalisedSpan[4..6]);
            match.DayTextSpan.CopyTo(normalisedSpan[6..8]);
            match.UniqueSpan.CopyTo(normalisedSpan[8..12]);
            normalised = new string(normalisedSpan);
        }
        result = TSelf.FromValidated(normalised);
        return true;
    }

    /// <inheritdoc />
    [Pure]
    public override string LongFormat() => _normalised;

    /// <inheritdoc />
    [Pure]
    public override string ShortFormat() => _normalised[2..];

    /// <inheritdoc />
    public override string Format(PnrFormat format) => FormatCore(format, SwedenClock.Today());

    /// <summary>
    ///     Formats using <paramref name="timeProvider"/> for centenarian "+"
    ///     separator inference.
    /// </summary>
    /// <param name="format">The desired output format.</param>
    /// <param name="timeProvider">Time provider for "as of" determination.</param>
    /// <returns>The formatted ID string.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="timeProvider"/> is <see langword="null"/>.
    /// </exception>
    [Pure]
    public string Format(PnrFormat format, TimeProvider timeProvider)
        => FormatCore(format, SwedenClock.Today(timeProvider));

    /// <summary>
    ///     Formats using <paramref name="today"/> as the reference date for
    ///     centenarian "+" inference. Fully deterministic — no clock read.
    /// </summary>
    /// <param name="format">The desired output format.</param>
    /// <param name="today">Reference date for separator inference.</param>
    /// <returns>The formatted ID string.</returns>
    [Pure]
    public string Format(PnrFormat format, DateOnly today) => FormatCore(format, today);

    private string FormatCore(PnrFormat format, DateOnly today)
    {
        // LongFormat returns the backing field directly — no allocation.
        if (format == PnrFormat.LongFormat) return _normalised;

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
    ///     Returns the canonical 12-digit normalised form. Round-trippable through
    ///     <see cref="PersonalId.Parse(string)"/> or
    ///     <see cref="CoordinationId.Parse(string)"/>.
    /// </summary>
    /// <returns>The canonical 12-digit YYYYMMDDXXXX string.</returns>
    public override string ToString() => _normalised;

    /// <summary>
    ///     Returns a culture-invariant <see cref="string"/> representation using
    ///     the given format specifier. See <see cref="TryFormat(Span{char}, out int, ReadOnlySpan{char}, IFormatProvider?)"/> for the
    ///     supported format strings.
    /// </summary>
    /// <param name="format">
    ///     The format string. Recognised values: <c>null</c> / <c>""</c> / <c>"L"</c>
    ///     = LongFormat; <c>"S"</c> = ShortFormat; <c>"LD"</c>/<c>"SD"</c>
    ///     = long/short with '-' separator; <c>"LI"</c>/<c>"SI"</c> = long/short
    ///     with inferred '+' (centenarian) or '-' separator.
    /// </param>
    /// <param name="formatProvider">Accepted and ignored — Swedish IDs are culture-invariant.</param>
    /// <returns>The formatted ID string.</returns>
    /// <exception cref="FormatException">When <paramref name="format"/> is not recognised.</exception>
    [Pure]
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        return Format(ParseFormatSpec(format.AsSpan()));
    }

    /// <summary>
    ///     Tries to format the ID into <paramref name="destination"/> as a span
    ///     of characters. Zero allocation when <paramref name="destination"/> is
    ///     large enough.
    /// </summary>
    /// <param name="destination">The destination buffer to write to.</param>
    /// <param name="charsWritten">The number of characters written on success.</param>
    /// <param name="format">
    ///     The format specifier. See <see cref="ToString(string?, IFormatProvider?)"/>
    ///     for the supported values.
    /// </param>
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
    ///     Writes the formatted ID into <paramref name="destination"/> using
    ///     <paramref name="today"/> as the reference date for centenarian-separator
    ///     inference. Deterministic — does not read any clock.
    /// </summary>
    /// <param name="destination">The destination buffer to write to.</param>
    /// <param name="format">The desired output format.</param>
    /// <param name="today">The reference date used for the centenarian '+' separator.</param>
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

    /// <summary>
    ///     Computes the exact output length for a given <see cref="PnrFormat"/>.
    /// </summary>
    private static int FormattedLength(PnrFormat format) => format switch
    {
        PnrFormat.LongFormat => 12,
        PnrFormat.ShortFormat => 10,
        PnrFormat.LongFormatWithSeparator => 13,
        PnrFormat.LongFormatWithStandardSeparator => 13,
        PnrFormat.ShortFormatWithSeparator => 11,
        PnrFormat.ShortFormatWithStandardSeparator => 11,
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
    };

    /// <summary>
    ///     Writes the formatted ID into <paramref name="destination"/> without
    ///     allocation. Returns <see langword="false"/> + zeroed <paramref name="charsWritten"/>
    ///     if the destination is too small.
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

        var src = _normalised.AsSpan();
        // Layout reference: src[0..4]=YYYY, src[4..6]=MM, src[6..8]=DD, src[8..12]=XXXX.
        switch (format)
        {
            case PnrFormat.LongFormat:
                src.CopyTo(destination);
                break;
            case PnrFormat.ShortFormat:
                src[2..].CopyTo(destination);
                break;
            case PnrFormat.LongFormatWithStandardSeparator:
                src[..8].CopyTo(destination);
                destination[8] = '-';
                src[8..12].CopyTo(destination[9..]);
                break;
            case PnrFormat.ShortFormatWithStandardSeparator:
                src[2..8].CopyTo(destination);
                destination[6] = '-';
                src[8..12].CopyTo(destination[7..]);
                break;
            case PnrFormat.LongFormatWithSeparator:
                src[..8].CopyTo(destination);
                destination[8] = GetAge(today) >= 100 ? '+' : '-';
                src[8..12].CopyTo(destination[9..]);
                break;
            case PnrFormat.ShortFormatWithSeparator:
                src[2..8].CopyTo(destination);
                destination[6] = GetAge(today) >= 100 ? '+' : '-';
                src[8..12].CopyTo(destination[7..]);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, null);
        }

        charsWritten = required;
        return true;
    }

    /// <summary>
    ///     Tries to format the ID into <paramref name="utf8Destination"/> as a
    ///     span of UTF-8 bytes. Swedish IDs are pure ASCII so the UTF-8 encoding
    ///     is one byte per character; zero allocation when the destination is
    ///     large enough.
    /// </summary>
    /// <param name="utf8Destination">The destination byte buffer.</param>
    /// <param name="bytesWritten">The number of bytes written on success.</param>
    /// <param name="format">The format specifier. See <see cref="ToString(string?, IFormatProvider?)"/>.</param>
    /// <param name="provider">Accepted and ignored — Swedish IDs are culture-invariant.</param>
    /// <returns>
    ///     <see langword="true"/> if the formatted value fit; otherwise
    ///     <see langword="false"/> with <paramref name="bytesWritten"/> = 0.
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
    ///     Writes the formatted ID into <paramref name="utf8Destination"/> as UTF-8
    ///     bytes using <paramref name="today"/> as the reference date for centenarian-
    ///     separator inference. Deterministic — does not read any clock.
    /// </summary>
    /// <param name="utf8Destination">The destination byte buffer.</param>
    /// <param name="format">The desired output format.</param>
    /// <param name="today">The reference date used for the centenarian '+' separator.</param>
    /// <param name="bytesWritten">The number of bytes written on success.</param>
    /// <returns>
    ///     <see langword="true"/> if the formatted value fit; otherwise
    ///     <see langword="false"/>.
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

        var src = _normalised.AsSpan();
        switch (format)
        {
            case PnrFormat.LongFormat:
                AsciiCharsToBytes(src, utf8Destination);
                break;
            case PnrFormat.ShortFormat:
                AsciiCharsToBytes(src[2..], utf8Destination);
                break;
            case PnrFormat.LongFormatWithStandardSeparator:
                AsciiCharsToBytes(src[..8], utf8Destination);
                utf8Destination[8] = (byte)'-';
                AsciiCharsToBytes(src[8..12], utf8Destination[9..]);
                break;
            case PnrFormat.ShortFormatWithStandardSeparator:
                AsciiCharsToBytes(src[2..8], utf8Destination);
                utf8Destination[6] = (byte)'-';
                AsciiCharsToBytes(src[8..12], utf8Destination[7..]);
                break;
            case PnrFormat.LongFormatWithSeparator:
                AsciiCharsToBytes(src[..8], utf8Destination);
                utf8Destination[8] = (byte)(GetAge(today) >= 100 ? '+' : '-');
                AsciiCharsToBytes(src[8..12], utf8Destination[9..]);
                break;
            case PnrFormat.ShortFormatWithSeparator:
                AsciiCharsToBytes(src[2..8], utf8Destination);
                utf8Destination[6] = (byte)(GetAge(today) >= 100 ? '+' : '-');
                AsciiCharsToBytes(src[8..12], utf8Destination[7..]);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, null);
        }

        bytesWritten = required;
        return true;
    }

    /// <summary>
    ///     Copies an ASCII char span to a byte span 1:1. Internal helper —
    ///     caller MUST guarantee <paramref name="source"/> contains only ASCII
    ///     (digits, '-', '+') and that <paramref name="destination"/> is long
    ///     enough.
    /// </summary>
    private static void AsciiCharsToBytes(ReadOnlySpan<char> source, Span<byte> destination)
    {
        for (var i = 0; i < source.Length; i++)
        {
            destination[i] = (byte)source[i];
        }
    }

    /// <summary>
    ///     Shared UTF-8 parse entry point: ASCII-validates the input bytes and
    ///     transcodes to a stackalloc char buffer before delegating to the
    ///     existing UTF-16 parser. Swedish IDs are pure ASCII; non-ASCII bytes
    ///     fail validation.
    /// </summary>
    /// <typeparam name="TSelf">The concrete person ID type.</typeparam>
    /// <param name="utf8Source">The candidate UTF-8 byte span.</param>
    /// <param name="currentYear">Reference year for century inference.</param>
    /// <param name="result">The parsed value on success.</param>
    /// <returns><see langword="true"/> on success.</returns>
    [Pure]
    [ContractAnnotation("=> true, result: notnull; => false, result: null")]
    private protected static bool TryParseUtf8Core<TSelf>(
        ReadOnlySpan<byte> utf8Source,
        int currentYear,
        [MaybeNullWhen(false)] out TSelf result)
        where TSelf : PhysicalPersonId, ISwedishPersonIdHooks<TSelf>
    {
        result = null;
        // Defensive bound — same as MaxInputLength used by the char path.
        if (utf8Source.Length is 0 or > 100) return false;
        Span<char> charBuffer = stackalloc char[utf8Source.Length];
        for (var i = 0; i < utf8Source.Length; i++)
        {
            var b = utf8Source[i];
            if (b > 127) return false; // non-ASCII not valid for Swedish IDs
            charBuffer[i] = (char)b;
        }
        // Delegate to the char-span parser via string allocation (parser entry
        // requires a string for fast-path identity reuse).
        var s = new string(charBuffer);
        return TryParseCore(s, currentYear, out result);
    }
}
