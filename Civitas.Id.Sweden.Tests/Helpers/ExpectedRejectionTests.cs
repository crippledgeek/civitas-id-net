using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Errors;

namespace Civitas.Id.Sweden.Tests.Helpers;

public class ExpectedRejectionTests
{
    [Test]
    public async Task NotRejected_TextOfNoType_IsEmpty()
    {
        await Assert.That(ExpectedRejection.NotRejected("not-an-id", ExpectedRejection.ByEveryType)).IsEmpty();
    }

    [Test]
    public async Task NotRejected_NumberOfThatType_NamesEveryEntryPointThatAcceptsIt()
    {
        var number = OfficialNumbers.Modern.Long;

        var failures = ExpectedRejection.NotRejected(number, ExpectedRejection.InvalidFormatFor<PersonalId>());

        await Assert.That(failures.Count).IsEqualTo(ParserProbe.AcceptedBy(number).Count(name => name.StartsWith("PersonalId/", StringComparison.Ordinal))
                                                    + ThrowingParserProbe.ReasonsFor(number).Count(asked => asked.Name.StartsWith("PersonalId/", StringComparison.Ordinal)));
        await Assert.That(failures.All(failure => failure.StartsWith("PersonalId/", StringComparison.Ordinal))).IsTrue();
        await Assert.That(failures.Any(failure => failure.Contains(number, StringComparison.Ordinal))).IsFalse();
    }

    [Test]
    public async Task NotRejected_OtherReason_NamesTheEntryPointsThatGiveAnother()
    {
        var failures = ExpectedRejection.NotRejected("not-an-id",
            new ExpectedRejection(typeof(PersonalId), InvalidIdNumberReason.InvalidChecksum));

        await Assert.That(failures.Count).IsEqualTo(ThrowingParserProbe.ReasonsFor("not-an-id")
            .Count(asked => asked.Name.StartsWith("PersonalId/", StringComparison.Ordinal)));
    }

    [Test]
    public async Task NotRejected_TypeNoProbeAsks_Throws()
    {
        await Assert.That(() => ExpectedRejection.NotRejected("not-an-id", new ExpectedRejection(typeof(string), InvalidIdNumberReason.Unknown)))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task NotRejected_EntryPointThatRejectsButLeavesAResult_IsNamed()
    {
        var results = new TypeResults(
            [new ProbeAnswer("T/string", Accepted: false, LeftResult: true), new ProbeAnswer("T/span", Accepted: false, LeftResult: false),
             new ProbeAnswer("T/valid", Accepted: false, LeftResult: null)],
            [("T/string", InvalidIdNumberReason.InvalidFormat)]);

        var failures = ExpectedRejection.ByEveryType[0].NotRejected(results);

        await Assert.That(failures.Count).IsEqualTo(1);
        await Assert.That(failures[0]).IsEqualTo("T/string rejects and leaves a result");
    }

    [Test]
    public async Task Ask_RejectingReadThatLeavesAResult_IsAskedAsLeavingOne()
    {
        var left = ParserProbe.Ask("T/string", (string _, out string? result) =>
        {
            result = "left";
            return false;
        }, "text");
        var none = ParserProbe.Ask("T/string", (string _, out string? result) =>
        {
            result = null;
            return false;
        }, "text");

        await Assert.That(left).IsEqualTo(new ProbeAnswer("T/string", Accepted: false, LeftResult: true));
        await Assert.That(none).IsEqualTo(new ProbeAnswer("T/string", Accepted: false, LeftResult: false));
    }
}
