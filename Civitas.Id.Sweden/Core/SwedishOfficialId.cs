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
public abstract record SwedishOfficialId
{
    /// <summary>Internal constructor — prevents external derivation.</summary>
    internal SwedishOfficialId()
    {
    }

    /// <summary>The ISO 3166-1 alpha-2 country code (always "SE" for this library).</summary>
    public static string CountryCode => "SE";

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

}
