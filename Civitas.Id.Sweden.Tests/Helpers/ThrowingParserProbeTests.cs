using Civitas.Id.Sweden.Core;

namespace Civitas.Id.Sweden.Tests.Helpers;

public class ThrowingParserProbeTests
{
    [Test]
    public async Task AsksEveryEntryPointThatThrows()
    {
        var methods = EntryPoints.Named(nameof(PersonalId.Parse), nameof(SwedishOfficialId.ParseAny));

        await Assert.That(ParseSignature.Differences(methods, ThrowingParserProbe.Covered)).IsEmpty();
    }

    [Test]
    public async Task ReasonsFor_OfficialNumber_HoldsTheOtherTypesOnly()
    {
        var rejected = ThrowingParserProbe.ReasonsFor(LegalPersonNumbers.Aktiebolag.Digits)
            .Where(asked => asked.Reason is not null).Select(asked => asked.Name).ToList();

        await Assert.That(rejected.Count(name => name.StartsWith("PersonalId/", StringComparison.Ordinal))).IsEqualTo(7);
        await Assert.That(rejected.Count(name => name.StartsWith("CoordinationId/", StringComparison.Ordinal))).IsEqualTo(7);
        await Assert.That(rejected.Count).IsEqualTo(14);
    }
}
