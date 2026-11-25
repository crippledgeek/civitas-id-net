using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Errors;
using JetBrains.Annotations;

namespace Civitas.Id.Sweden.Fakers;

/// <summary>
///     Faker contract for Swedish person IDs (personnummer / samordningsnummer)
///     — adds gender-specific generation and centenarian support.
/// </summary>
/// <typeparam name="T">The concrete <see cref="PhysicalPersonId" /> subtype.</typeparam>
[PublicAPI]
public interface IPersonIdFaker<out T> : IIdFaker<T>
    where T : PhysicalPersonId
{
    /// <summary>Generates a valid ID with the gender digit set to an odd value.</summary>
    T GenerateMale();

    /// <summary>Generates a valid ID with the gender digit set to an even value.</summary>
    T GenerateFemale();

    /// <summary>
    ///     Generates a valid male ID for the specified <paramref name="birthDate" />.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when <paramref name="birthDate" /> is before 1800.
    /// </exception>
    T GenerateMale(DateOnly birthDate);

    /// <summary>
    ///     Generates a valid male ID for the specified date components.
    /// </summary>
    /// <exception cref="InvalidIdNumberException">
    ///     Thrown when the date components do not form a valid calendar date.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when the components form a calendar date before 1800.
    /// </exception>
    T GenerateMale(int year, int month, int day);

    /// <summary>
    ///     Generates a valid female ID for the specified <paramref name="birthDate" />.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when <paramref name="birthDate" /> is before 1800.
    /// </exception>
    T GenerateFemale(DateOnly birthDate);

    /// <summary>
    ///     Generates a valid female ID for the specified date components.
    /// </summary>
    /// <exception cref="InvalidIdNumberException">
    ///     Thrown when the date components do not form a valid calendar date.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when the components form a calendar date before 1800.
    /// </exception>
    T GenerateFemale(int year, int month, int day);

    /// <summary>
    ///     Generates a valid ID for a person aged 100–110 years (inclusive)
    ///     as of "today" in Sweden's civil timezone (<c>Europe/Stockholm</c>).
    /// </summary>
    /// <param name="timeProvider">
    ///     The time provider used to determine "today". <see langword="null" />
    ///     falls back to <see cref="TimeProvider.System" />. The provider's
    ///     <see cref="TimeProvider.LocalTimeZone" /> is intentionally ignored;
    ///     the timezone is library-fixed to <c>Europe/Stockholm</c>.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when "today" is before 1900, as everyone that old was then born before 1800. From 1900 to 1909
    ///     the ages end where the birth year would be before 1800.
    /// </exception>
    T GenerateCentenarian(TimeProvider? timeProvider = null);
}
