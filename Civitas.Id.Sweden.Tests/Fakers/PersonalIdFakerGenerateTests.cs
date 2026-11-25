using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Fakers;

namespace Civitas.Id.Sweden.Tests.Fakers;

[InheritsTests]
public sealed class PersonalIdFakerGenerateTests : PersonFakerGenerateTests
{
    private protected override PersonGeneration Create(int seed)
    {
        var faker = new PersonalIdFaker(seed);
        return new PersonGeneration
        {
            Generate = faker.Generate,
            GenerateDefect = faker.Generate,
            SupportedDefects = faker.SupportedDefects,
            Parse = PersonalId.Parse
        };
    }
}
