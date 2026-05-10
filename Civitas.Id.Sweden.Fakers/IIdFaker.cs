using System.Security.Cryptography;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Errors;
using JetBrains.Annotations;

namespace Civitas.Id.Sweden.Fakers;

/// <summary>
///     Base contract for fakers that generate valid Swedish official ID numbers.
/// </summary>
/// <remarks>
///     <para>
///         The default constructor on every concrete faker uses a cryptographically
///         secure random number generator (<see cref="RandomNumberGenerator" />).
///     </para>
///     <para>
///         The <c>(int seed)</c> and <c>(Random)</c> constructor overloads exist
///         for deterministic test fixtures only. Generated IDs from those paths are
///         reproducible for the same seed but use a non-cryptographic PRNG. Do NOT
///         rely on the seeded path for security-sensitive scenarios.
///     </para>
/// </remarks>
/// <typeparam name="T">The concrete <see cref="SwedishOfficialId" /> subtype produced.</typeparam>
[PublicAPI]
public interface IIdFaker<out T>
    where T : SwedishOfficialId
{
    /// <summary>The ISO 3166-1 alpha-2 country code. Always <c>"SE"</c>.</summary>
    string CountryCode { get; }

    /// <summary>
    ///     Generates a valid ID with a uniformly-random birth date in
    ///     [1970-01-01, 2019-12-31] (sibling-parity range).
    /// </summary>
    T Generate();

    /// <summary>Generates a valid ID for the given <paramref name="birthDate" />.</summary>
    /// <exception cref="InvalidIdNumberException">
    ///     Thrown when the date cannot form a valid Swedish ID (invalid for the subtype).
    /// </exception>
    T Generate(DateOnly birthDate);

    /// <summary>
    ///     Generates a valid ID for the given date components.
    /// </summary>
    /// <exception cref="InvalidIdNumberException">
    ///     Thrown when the date components do not form a valid calendar date.
    /// </exception>
    T Generate(int year, int month, int day);
}
