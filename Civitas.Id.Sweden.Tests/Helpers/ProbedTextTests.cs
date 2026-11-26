using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Errors;

namespace Civitas.Id.Sweden.Tests.Helpers;

public class ProbedTextTests
{
    [Test]
    public async Task Of_AsksEachProbeOnceForTheText()
    {
        var triesAsked = 0;
        var throwsAsked = 0;

        var probed = ProbedText.Of("not-an-id",
            text =>
            {
                triesAsked++;
                return ParserProbe.Answers(text);
            },
            text =>
            {
                throwsAsked++;
                return ThrowingParserProbe.ReasonsFor(text);
            });
        var failures = ExpectedRejection.NotRejected(probed, ExpectedRejection.ByEveryType);
        var notAccepted = ExpectedAcceptance.NotAccepted(probed, typeof(PersonalId), typeof(SwedishOfficialId));

        await Assert.That(failures).IsEmpty();
        await Assert.That(notAccepted).IsNotEmpty();
        await Assert.That(triesAsked).IsEqualTo(1);
        await Assert.That(throwsAsked).IsEqualTo(1);
    }

    [Test]
    public async Task For_TypeNoProbeAsks_Throws()
    {
        var probed = ProbedText.Of("not-an-id");

        await Assert.That(() => probed.For(typeof(string))).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task For_KeepsOnlyTheEntryPointsOfTheType()
    {
        var results = ProbedText.Of("not-an-id").For(typeof(CoordinationId));

        await Assert.That(results.Tries.All(answer => answer.Name.StartsWith("CoordinationId/", StringComparison.Ordinal))).IsTrue();
        await Assert.That(results.Throws.All(answer => answer.Name.StartsWith("CoordinationId/", StringComparison.Ordinal))).IsTrue();
        await Assert.That(results.Tries.Count).IsGreaterThan(0);
        await Assert.That(results.Throws.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task Of_ReasonsAreThoseTheThrowingProbeGives()
    {
        var results = ProbedText.Of("not-an-id").For(typeof(PersonalId));

        await Assert.That(results.Throws.All(answer => answer.Reason == InvalidIdNumberReason.InvalidFormat)).IsTrue();
    }
}
