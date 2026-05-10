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
    T GenerateMale(DateOnly birthDate);

    /// <summary>
    ///     Generates a valid male ID for the specified date components.
    /// </summary>
    /// <exception cref="InvalidIdNumberException">
    ///     Thrown when the date components do not form a valid calendar date.
    /// </exception>
    T GenerateMale(int year, int month, int day);

    /// <summary>
    ///     Generates a valid female ID for the specified <paramref name="birthDate" />.
    /// </summary>
    T GenerateFemale(DateOnly birthDate);

    /// <summary>
    ///     Generates a valid female ID for the specified date components.
    /// </summary>
    /// <exception cref="InvalidIdNumberException">
    ///     Thrown when the date components do not form a valid calendar date.
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
    T GenerateCentenarian(TimeProvider? timeProvider = null);
}
