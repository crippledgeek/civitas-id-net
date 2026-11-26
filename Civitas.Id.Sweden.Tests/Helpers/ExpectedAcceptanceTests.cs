using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Errors;

namespace Civitas.Id.Sweden.Tests.Helpers;

public class ExpectedAcceptanceTests
{
    [Test]
    public async Task NotAccepted_NumberOfThoseTypes_IsEmpty()
    {
        var number = OfficialNumbers.Modern.Long;

        await Assert.That(ExpectedAcceptance.NotAccepted(number, typeof(PersonalId), typeof(OrganisationId), typeof(SwedishOfficialId)))
            .IsEmpty();
    }

    [Test]
    public async Task NotAccepted_NumberOfAnotherType_NamesEveryEntryPointOfThatType()
    {
        var number = OfficialNumbers.Modern.Long;

        var failures = ExpectedAcceptance.NotAccepted(number, typeof(CoordinationId));

        await Assert.That(failures.Count).IsEqualTo(ParserProbe.Answers(number).Count(answer => answer.Name.StartsWith("CoordinationId/", StringComparison.Ordinal))
                                                    + ThrowingParserProbe.ReasonsFor(number).Count(asked => asked.Name.StartsWith("CoordinationId/", StringComparison.Ordinal)));
        await Assert.That(failures.All(failure => failure.StartsWith("CoordinationId/", StringComparison.Ordinal))).IsTrue();
        await Assert.That(failures.Any(failure => failure.Contains(number, StringComparison.Ordinal))).IsFalse();
    }

    [Test]
    public async Task NotAccepted_TypeNoProbeAsks_Throws()
    {
        await Assert.That(() => ExpectedAcceptance.NotAccepted(OfficialNumbers.Modern.Long, typeof(string)))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task NotAccepted_EntryPointsThatAcceptWithoutAResultOrThrow_AreNamed()
    {
        var results = new TypeResults(
            [new ProbeAnswer("T/string", Accepted: true, LeftResult: false), new ProbeAnswer("T/span", Accepted: false, LeftResult: false),
             new ProbeAnswer("T/utf8", Accepted: true, LeftResult: true), new ProbeAnswer("T/valid", Accepted: true, LeftResult: null)],
            [("T/string", InvalidIdNumberReason.InvalidFormat), ("T/span", null)]);

        var failures = ExpectedAcceptance.NotAccepted(results);

        await Assert.That(failures.Count).IsEqualTo(3);
        await Assert.That(failures).Contains("T/string accepts and leaves no result");
        await Assert.That(failures).Contains("T/span rejects");
        await Assert.That(failures).Contains("T/string throws InvalidFormat");
    }
}
