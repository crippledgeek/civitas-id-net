using System.Globalization;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Format;
using Civitas.Id.Sweden.Tests.Helpers;

namespace Civitas.Id.Sweden.Tests;

/// <summary>
///     Fixture-driven tests over the test data files read below. THIRD-PARTY-DATA.md says which of them
///     come from Skatteverket. The official 2019 samordningsnummer file is not asserted here.
/// </summary>
public class FixtureDrivenTests
{
    /// <summary>
    ///     Reference "today" for samordningsnummer age assertions. Calibrated
    ///     empirically against Testsamordningsnummer_2019_corrected.csv: the
    ///     unique date satisfying every valid row is 2025-05-01.
    /// </summary>
    private static readonly DateOnly SamordningsnummerFixtureToday = new(2025, 5, 1);

    // ── Samordningsnummer (column order differs!) ──

    public static IEnumerable<(string Pin, bool Female, bool Male, int Age, bool Adult, bool Child, bool Valid)>
        Samordningsnummer()
    {
        var rows = CsvLoader.Rows(Fixtures.SamordningsnummerCorrected).Select(row => row.Cells);
        foreach (var r in rows)
            yield return (
                r[0],
                bool.Parse(r[1]),
                bool.Parse(r[2]),
                int.Parse(r[3], CultureInfo.InvariantCulture),
                bool.Parse(r[4]),
                bool.Parse(r[5]),
                bool.Parse(r[6]));
    }

    [Test]
    [MethodDataSource(nameof(Samordningsnummer))]
    public async Task CoordinationId_FixtureMatchesAllColumns(
        string pin, bool female, bool male, int age, bool adult, bool child, bool valid)
    {
        await Assert.That(CoordinationId.IsValid(pin)).IsEqualTo(valid);
        if (!valid) return;

        var id = CoordinationId.Parse(pin);
        await Assert.That(id.IsFemale).IsEqualTo(female);
        await Assert.That(id.IsMale).IsEqualTo(male);
        await Assert.That(id.GetAge(SamordningsnummerFixtureToday)).IsEqualTo(age);
        await Assert.That(id.IsAdult(SamordningsnummerFixtureToday)).IsEqualTo(adult);
        await Assert.That(id.IsChild(SamordningsnummerFixtureToday)).IsEqualTo(child);
    }

    // ── Official 2020 samordningsnummer ──

    public static IEnumerable<string> Samordningsnummer2020()
    {
        return CsvLoader.Column(Fixtures.Samordningsnummer2020);
    }

    [Test]
    [MethodDataSource(nameof(Samordningsnummer2020))]
    public async Task CoordinationId_2020Fixture_KeepsNumberAndBirthDate(string pin)
    {
        var id = CoordinationId.Parse(pin);
        await Assert.That(id.LongFormat()).IsEqualTo(pin);
        await Assert.That(id.BirthDate).IsEqualTo(new OfficialSamordningsnummer(pin).BirthDate);
    }

    // ── Organisationsnummer extended, and the sole-trader rows without the columns that drop the century ──

    private const int OrganisationLength = 10;

    /// <summary>
    ///     Where one organisation row is, only ever printed, and what it expects: the file's cells as raw text, with
    ///     the short form taken from the input, and no VAT number, where the file has no such column.
    /// </summary>
    public sealed record OrganisationRow
    {
        public required string Source { get; init; }
        public required string Input { get; init; }
        public required string LongFormat { get; init; }
        public required string ShortFormat { get; init; }
        public string? VatNumber { get; init; }
        public required bool Valid { get; init; }
        public required string Type { get; init; }

        /// <summary>The place of the row, because a short form can read as another person's number.</summary>
        public override string ToString()
        {
            return Source;
        }
    }

    public static IEnumerable<OrganisationRow> OrganisationsnummerRows()
    {
        return
        [
            .. CsvLoader.Rows(Fixtures.OrganisationsnummerExtended).Select(row =>
                new OrganisationRow
                {
                    Source = SourceOf(row),
                    Input = row.Cells[0],
                    LongFormat = row.Cells[1],
                    ShortFormat = row.Cells[2],
                    Valid = bool.Parse(row.Cells[3]),
                    Type = row.Cells[4],
                    VatNumber = row.Cells[5]
                }),
            .. CsvLoader.Rows(Fixtures.OrganisationsnummerEnskildFirma).Select(row =>
                new OrganisationRow
                {
                    Source = SourceOf(row),
                    Input = row.Cells[0],
                    LongFormat = row.Cells[1],
                    ShortFormat = LastTenDigits(row.Cells[0]),
                    Valid = bool.Parse(row.Cells[2]),
                    Type = row.Cells[3]
                })
        ];
    }

    private static string SourceOf(FixtureRow row)
    {
        return $"{row.FileName}:{row.Line}";
    }

    private static string LastTenDigits(string input)
    {
        return string.Concat(input.Where(char.IsAsciiDigit))[^OrganisationLength..];
    }

    [Test]
    [MethodDataSource(nameof(OrganisationsnummerRows))]
    public async Task OrganisationId_FixtureMatchesShape(OrganisationRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        await Assert.That(OrganisationId.IsValid(row.Input)).IsEqualTo(row.Valid);
        if (!row.Valid) return;

        var id = OrganisationId.Parse(row.Input);
        await UnprintedAssert.AreEqual(id.ShortFormat(), row.ShortFormat).ConfigureAwait(false);
        if (row.VatNumber is not null)
            await UnprintedAssert.AreEqual($"SE{id.ShortFormat()}01", row.VatNumber).ConfigureAwait(false);
        // The files' "long_format" column is the short form with a separator: "-", or "+" for an Enskild firma
        // whose bearer is 100 or older. ShortFormatWithSeparator infers the same separator.
        await UnprintedAssert.AreEqual(id.Format(PnrFormat.ShortFormatWithSeparator), row.LongFormat)
            .ConfigureAwait(false);

        // The files' "type" column is the layperson entity name ("Aktiebolag"
        // for legal persons, "Enskild firma" for physical persons). Map and assert.
        var expectedNumberType = row.Type switch
        {
            "Aktiebolag" => OrganisationNumberType.LegalPerson,
            "Enskild firma" => OrganisationNumberType.PhysicalPerson,
            _ => throw new InvalidOperationException($"Unknown type column value: {row.Type}")
        };
        await Assert.That(id.NumberType).IsEqualTo(expectedNumberType);
    }

    // ── Organisationsnummer textual variants (no header, multi-format same number) ──

    public static IEnumerable<string> OrganisationsnummerTextual()
    {
        return CsvLoader.Column(Fixtures.OrganisationsnummerTextual);
    }

    [Test]
    [MethodDataSource(nameof(OrganisationsnummerTextual))]
    public async Task OrganisationId_TextualVariants_AllParse(string input)
    {
        await Assert.That(OrganisationId.IsValid(input)).IsTrue();
    }

    // ── Explicit invalid set ──

    public static IEnumerable<string> InvalidIds()
    {
        return CsvLoader.Column(Fixtures.InvalidIds);
    }

    [Test]
    [MethodDataSource(nameof(InvalidIds))]
    public async Task InvalidIds_RejectedByAllParsers(string id)
    {
        await Assert.That(SwedishOfficialId.IsValid(id)).IsFalse();
    }
}
