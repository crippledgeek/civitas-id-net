using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Fakers;

namespace Civitas.Id.Sweden.Tests.Fakers;

[InheritsTests]
public sealed class CoordinationIdFakerGenerateTests : PersonFakerGenerateTests
{
    private protected override PersonGeneration Create(int seed)
    {
        var faker = new CoordinationIdFaker(seed);
        return new PersonGeneration
        {
            Generate = faker.Generate,
            GenerateDefect = faker.Generate,
            SupportedDefects = faker.SupportedDefects,
            Parse = CoordinationId.Parse
        };
    }
}
