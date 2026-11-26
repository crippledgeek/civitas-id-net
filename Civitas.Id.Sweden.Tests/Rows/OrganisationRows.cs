using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Format;
using Civitas.Id.Sweden.Tests.Helpers;

namespace Civitas.Id.Sweden.Tests.Rows;

/// <summary>
///     One test per row of the three organisation number files: <c>testorganisationsnummer_extended.csv</c> (valid and
///     invalid rows), <c>testorganisationsnummer_enskild_firma.csv</c> (sole traders written as organisation numbers,
///     all valid) and <c>testorganisationsnummer.txt</c> (textual variants of legal persons' numbers).
/// </summary>
public class OrganisationRows
{
    public static IEnumerable<OrganisationRow> ExtendedValid()
    {
        return FileRows.ValidExtendedOrganisations();
    }

    public static IEnumerable<OrganisationRow> ExtendedInvalid()
    {
        return FileRows.InvalidExtendedOrganisations();
    }

    public static IEnumerable<OrganisationRow> SoleTraders()
    {
        return FileRows.SoleTraders();
    }

    public static IEnumerable<NumberRow> Textual()
    {
        return FileRows.Numbers(Fixtures.OrganisationsnummerTextual);
    }

    [Test]
    [MethodDataSource(nameof(ExtendedValid))]
    public async Task Accepted_WithEveryColumn(OrganisationRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        await OrganisationCells.AgreeWithVatAsync(row).ConfigureAwait(false);
        await AssertAcceptedAsync(row).ConfigureAwait(false);
    }

    [Test]
    [MethodDataSource(nameof(SoleTraders))]
    public async Task SoleTrader_Accepted_WithEveryColumn(OrganisationRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        await OrganisationCells.AgreeAsync(row).ConfigureAwait(false);
        await AssertAcceptedAsync(row).ConfigureAwait(false);
    }

    [Test]
    [MethodDataSource(nameof(ExtendedInvalid))]
    public async Task Rejected_ByEveryParser_AndFailsTheCheckDigit(OrganisationRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        await OrganisationCells.AgreeWithVatAsync(row).ConfigureAwait(false);

        await Assert.That(IndependentLuhn.IsValid(WrittenNumber.Digits(row.Input))).IsFalse();
        await Assert.That(ExpectedRejection.NotRejected(row.Input, ExpectedRejection.ByEveryType)).IsEmpty();
    }

    [Test]
    [MethodDataSource(nameof(SoleTraders))]
    public async Task SoleTrader_IsAnOfficialPersonNumberWithItsCentury(OrganisationRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        var parsed = OrganisationId.TryParse(row.Input, ReferenceDates.SoleTrader, out var id);
        await Assert.That(parsed).IsTrue();
        ArgumentNullException.ThrowIfNull(id);
        var person = id.ToPhysicalPersonId();
        ArgumentNullException.ThrowIfNull(person);

        var digits = WrittenNumber.Digits(row.Input);
        await UnprintedAssert.AreEqual(person.LongFormat(), digits).ConfigureAwait(false);
        await Assert.That(OfficialNumbers.All.Any(number => number.Long == digits)).IsTrue();
    }

    [Test]
    public async Task SoleTraders_ReferenceDate_IsInsideTheWindowThatTheSeparatorsAllow()
    {
        var rows = SoleTraders().ToList();
        var born = rows.ToDictionary(row => row,
            row => OfficialNumbers.All.First(number => number.Long == WrittenNumber.Digits(row.Input)).BirthDate);
        var turned100 = (OrganisationRow row) => born[row].AddYears(100);

        var plus = rows.Where(row => row.LongFormat.Contains('+', StringComparison.Ordinal)).ToList();
        var hyphen = rows.Where(row => row.LongFormat.Contains('-', StringComparison.Ordinal)).ToList();

        await Assert.That(plus.Count).IsGreaterThan(0);
        await Assert.That(hyphen.Count).IsGreaterThan(0);
        await Assert.That(plus.Count + hyphen.Count).IsEqualTo(rows.Count);
        await Assert.That(ReferenceDates.SoleTrader >= plus.Max(turned100)).IsTrue();
        await Assert.That(ReferenceDates.SoleTrader < hyphen.Min(turned100)).IsTrue();
    }

    [Test]
    [MethodDataSource(nameof(Textual))]
    public async Task Accepted_AsLegalPerson(NumberRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        await Assert.That(ExpectedAcceptance.NotAccepted(row.Number, typeof(OrganisationId), typeof(SwedishOfficialId)))
            .IsEmpty();
        var parsed = SwedishOfficialId.TryParseAny(row.Number, ReferenceDates.Undated, out var any);
        await Assert.That(parsed).IsTrue();
        await Assert.That(any).IsTypeOf<OrganisationId>();

        var id = (OrganisationId)any!;
        await UnprintedAssert.AreEqual(id.LongFormat(), WrittenNumber.LastTenDigits(row.Number)).ConfigureAwait(false);
        await Assert.That(id.NumberType).IsEqualTo(OrganisationNumberType.LegalPerson);
    }

    [Test]
    [MethodDataSource(nameof(Textual))]
    public async Task Rejected_AsPerson(NumberRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        await Assert.That(ExpectedRejection.NotRejected(row.Number, ExpectedRejection.InvalidFormatFor<PersonalId>(),
            ExpectedRejection.InvalidFormatFor<CoordinationId>())).IsEmpty();
    }

    private static async Task AssertAcceptedAsync(OrganisationRow row)
    {
        await Assert.That(ExpectedAcceptance.NotAccepted(row.Input, typeof(OrganisationId), typeof(SwedishOfficialId)))
            .IsEmpty();

        var parsed = OrganisationId.TryParse(row.Input, ReferenceDates.SoleTrader, out var id);
        await Assert.That(parsed).IsTrue();
        ArgumentNullException.ThrowIfNull(id);

        await UnprintedAssert.AreEqual(id.ShortFormat(), row.ShortFormat).ConfigureAwait(false);
        await UnprintedAssert.AreEqual(id.LongFormat(), row.ShortFormat).ConfigureAwait(false);
        // The file's long format is the short form with "-", or with "+" for a sole trader whose bearer is 100 or older.
        await UnprintedAssert.AreEqual(id.Format(PnrFormat.ShortFormatWithSeparator, ReferenceDates.SoleTrader), row.LongFormat)
            .ConfigureAwait(false);
        await Assert.That(id.NumberType).IsEqualTo(OrganisationCells.NumberTypeOf(row.Type));
    }
}
