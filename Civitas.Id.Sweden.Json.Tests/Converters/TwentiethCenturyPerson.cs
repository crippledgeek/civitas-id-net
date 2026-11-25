using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Fakers;

namespace Civitas.Id.Sweden.Json.Tests.Converters;

/// <summary>
///     A generated person whose short form resolves to the 1900s. No pinned set holds a personnummer of 1926-1999. The
///     number may belong to someone, so tests do not print it.
/// </summary>
internal static class TwentiethCenturyPerson
{
    private const int FakerSeed = 1;

    public static readonly DateOnly BirthDate = new(1981, 12, 18);

    public static PersonalId Create()
    {
        return PersonalIdFaker.WithSeed(FakerSeed).Generate(BirthDate);
    }
}
