using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Fakers;
using Civitas.Id.Sweden.Fakers.Internal;

namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>
///     Numbers that no pinned set holds, generated from one seed. A number may belong to someone, so tests do not
///     print it. The texts are <see cref="string" /> because they are raw parser input.
/// </summary>
internal static class GeneratedNumbers
{
    private const int Seed = 1;
    private const int PersonDefects = 9;

    /// <summary>A date whose short form resolves to the 1900s. No pinned set has a personnummer of 1926-1999.</summary>
    public static readonly DateOnly TwentiethCentury = new(1981, 12, 18);

    /// <summary>A valid personnummer as text.</summary>
    public static string ValidPersonnummer => PersonalIdFaker.WithSeed(Seed).Generate(IdValidity.Valid).Text;

    /// <summary>One invalid personnummer for each defect.</summary>
    /// <exception cref="InvalidOperationException">The number of defects is not the pinned one.</exception>
    public static IReadOnlyList<string> InvalidPersonnummer()
    {
        var faker = PersonalIdFaker.WithSeed(Seed);
        return faker.SupportedDefects.Count == PersonDefects
            ? [.. faker.SupportedDefects.Select(defect => faker.Generate(defect).Text)]
            : throw new InvalidOperationException($"{faker.SupportedDefects.Count} defects, expected {PersonDefects}.");
    }

    /// <summary>A person born on <paramref name="birthDate" />.</summary>
    public static PersonalId Person(DateOnly birthDate)
    {
        return PersonalIdFaker.WithSeed(Seed).Generate(birthDate);
    }

    /// <summary>A person with a samordningsnummer, born on <paramref name="birthDate" />.</summary>
    public static CoordinationId CoordinationPerson(DateOnly birthDate)
    {
        return CoordinationIdFaker.WithSeed(Seed).Generate(birthDate);
    }
}
