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
    /// <summary>The valid rows of the extended file.</summary>
    public abstract class ExtendedValid : IRowSource<OrganisationRow>
    {
        public static FixtureSpec File => Fixtures.OrganisationsnummerExtended;
        public static int Pinned => 6;
        public static int Consumers => 1;

        public static IEnumerable<OrganisationRow> Read()
        {
            return FileRows.ValidExtendedOrganisations();
        }
    }

    /// <summary>The rows of the extended file that are not valid.</summary>
    public abstract class ExtendedInvalid : IRowSource<OrganisationRow>
    {
        public static FixtureSpec File => Fixtures.OrganisationsnummerExtended;
        public static int Pinned => 4;
        public static int Consumers => 1;

        public static IEnumerable<OrganisationRow> Read()
        {
            return FileRows.InvalidExtendedOrganisations();
        }
    }

    /// <summary>The rows of the sole-trader file.</summary>
    public abstract class SoleTraders : IRowSource<OrganisationRow>
    {
        public static FixtureSpec File => Fixtures.OrganisationsnummerEnskildFirma;
        public static int Pinned => 20;
        public static int Consumers => 2;

        public static IEnumerable<OrganisationRow> Read()
        {
            return FileRows.SoleTraders();
        }
    }

    /// <summary>The numbers of the textual file.</summary>
    public abstract class Textual : IRowSource<NumberRow>
    {
        public static FixtureSpec File => Fixtures.OrganisationsnummerTextual;
        public static int Pinned => 6;
        public static int Consumers => 2;

        public static IEnumerable<NumberRow> Read()
        {
            return FileRows.Numbers(File);
        }
    }

    [Test]
    [RowsOf<ExtendedValid, OrganisationRow>]
    public async Task Accepted_WithEveryColumn(OrganisationRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        await OrganisationCells.AgreeWithVatAsync(row).ConfigureAwait(false);
        await AssertAcceptedAsync(row).ConfigureAwait(false);
    }

    [Test]
    [RowsOf<SoleTraders, OrganisationRow>]
    public async Task SoleTrader_Accepted_WithEveryColumn(OrganisationRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        await OrganisationCells.AgreeAsync(row).ConfigureAwait(false);
        await AssertAcceptedAsync(row).ConfigureAwait(false);
    }

    [Test]
    [RowsOf<ExtendedInvalid, OrganisationRow>]
    public async Task Rejected_ByEveryParser_AndFailsTheCheckDigit(OrganisationRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        await OrganisationCells.AgreeWithVatAsync(row).ConfigureAwait(false);

        await Assert.That(IndependentLuhn.IsValid(WrittenNumber.Digits(row.Input))).IsFalse();
        await Assert.That(ExpectedRejection.NotRejected(row.Input, ExpectedRejection.ByEveryType)).IsEmpty();
    }

    [Test]
    [RowsOf<SoleTraders, OrganisationRow>]
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
        var rows = SoleTraders.Read().ToList();
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
    [RowsOf<Textual, NumberRow>]
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
    [RowsOf<Textual, NumberRow>]
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
