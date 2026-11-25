using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Fakers;

namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>
///     Numbers that no pinned set holds, generated from one seed. A number may belong to someone, so tests do not
///     print it.
/// </summary>
internal static class GeneratedNumbers
{
    private const int Seed = 1;

    /// <summary>A date whose short form resolves to the 1900s. No pinned set has a personnummer of 1926-1999.</summary>
    public static readonly DateOnly TwentiethCentury = new(1981, 12, 18);

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
