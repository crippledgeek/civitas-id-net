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
public abstract record PhysicalPersonId : SwedishOfficialId
{
    /// <summary>Canonical 12-digit normalised form (YYYYMMDDXXXX).</summary>
    /// <remarks>
    ///     Non-positional <see langword="private protected"/> field. By design it
    ///     does NOT appear in synthesised <see cref="object.ToString()"/> output
    ///     (records' <c>PrintMembers</c> emits public members only). The overridden
    ///     <see cref="ToString"/> returns this value directly per the
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

    /// <summary>Returns "+" if age &gt;= 100 on the given date, otherwise "-".</summary>
    private string InferSeparator(DateOnly today)
    {
        return GetAge(today) >= 100 ? "+" : "-";
    }

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

    /// <summary>
    ///     Returns the canonical 12-digit normalised form. Round-trippable through
    ///     <see cref="PersonalId.Parse(string)"/> or
    ///     <see cref="CoordinationId.Parse(string)"/>.
    /// </summary>
    /// <returns>The canonical 12-digit YYYYMMDDXXXX string.</returns>
    public override string ToString() => _normalised;
}
