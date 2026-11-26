using Civitas.Id.Sweden.Core;

namespace Civitas.Id.Sweden.Tests.Helpers;

public class ParserProbeTests
{
    [Test]
    public async Task AsksEveryEntryPointThatDoesNotThrow()
    {
        var methods = EntryPoints.Named(nameof(PersonalId.TryParse), nameof(SwedishOfficialId.TryParseAny),
            nameof(SwedishOfficialId.IsValid));

        await Assert.That(ParseSignature.Differences(methods, ParserProbe.Covered)).IsEmpty();
    }

    [Test]
    public async Task AcceptedBy_OfficialNumber_HoldsItsOwnEntryPointsOnly()
    {
        var accepted = ParserProbe.AcceptedBy(LegalPersonNumbers.Aktiebolag.Digits);

        await Assert.That(accepted.Count(name => name.StartsWith("OrganisationId/", StringComparison.Ordinal))).IsEqualTo(7);
        await Assert.That(accepted.Count(name => name.StartsWith("SwedishOfficialId/", StringComparison.Ordinal))).IsEqualTo(5);
        await Assert.That(accepted.Count).IsEqualTo(12);
    }

    [Test]
    public async Task AcceptedBy_NoNumber_IsEmpty()
    {
        await Assert.That(ParserProbe.AcceptedBy("not-an-id")).IsEmpty();
    }
}
