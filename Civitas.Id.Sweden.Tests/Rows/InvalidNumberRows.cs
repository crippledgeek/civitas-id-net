using Civitas.Id.Sweden.Tests.Helpers;

namespace Civitas.Id.Sweden.Tests.Rows;

/// <summary>
///     One test per row of <c>invalid_swedish_ids.csv</c>: each is rejected by every parser, for the reason pinned for
///     its line. Lines 3 and 4 hold the check digit; the others fail it as well, so the check digit is not what
///     rejects them. That a date which does not exist is rejected for that reason alone is proven by the generated
///     defects, not here.
/// </summary>
public class InvalidNumberRows
{
    public static IEnumerable<InvalidNumberRow> Rows()
    {
        return FileRows.InvalidNumbers();
    }

    [Test]
    [MethodDataSource(nameof(Rows))]
    public async Task Rejected_ByEveryParser(InvalidNumberRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        await Assert.That(ExpectedRejection.NotRejected(row.Number, ExpectedRejection.ByEveryTypeAs(row.Reason))).IsEmpty();
    }

    [Test]
    [MethodDataSource(nameof(Rows))]
    public async Task ChecksumNote_MatchesTheDigits(InvalidNumberRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        await Assert.That(!IndependentLuhn.IsValid(WrittenNumber.Digits(row.Number))).IsEqualTo(row.FailsChecksum);
    }

    [Test]
    public async Task EveryLineOfTheFile_HasAnExpectation()
    {
        var lines = CsvLoader.Rows(Fixtures.InvalidIds).Select(row => row.Line).Order();
        await Assert.That(lines.SequenceEqual(InvalidNumberExpectations.Lines.Order())).IsTrue();
    }
}
