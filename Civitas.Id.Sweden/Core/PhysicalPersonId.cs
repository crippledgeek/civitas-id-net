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
    /// <summary>Internal constructor — prevents external derivation.</summary>
    internal PhysicalPersonId()
    {
    }

    /// <summary>The decoded date of birth for this ID.</summary>
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
    private protected string InferSeparator(DateOnly today)
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
}
