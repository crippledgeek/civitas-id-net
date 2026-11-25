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
    ///     Reference "today" for personnummer age assertions. Calibrated empirically
    ///     by intersecting (yyyy+age, mm, dd) bounds across every valid row of
    ///     testpersonnummer_final.csv: the unique date satisfying every row is
    ///     2025-04-23. The two derived CSVs (numbers from Skatteverket, other
    ///     columns added) use different reference dates, so each has its own.
    /// </summary>
    private static readonly DateOnly PersonnummerFixtureToday = new(2025, 4, 23);

    /// <summary>
    ///     Reference "today" for samordningsnummer age assertions. Calibrated
    ///     empirically against Testsamordningsnummer_2019_corrected.csv: the
    ///     unique date satisfying every valid row is 2025-05-01.
    /// </summary>
    private static readonly DateOnly SamordningsnummerFixtureToday = new(2025, 5, 1);

    // ── Personnummer 1890s (single column, no header) ──

    public static IEnumerable<string> Personnummer1890S()
    {
        return CsvLoader.Column(Fixtures.Personnummer1890S);
    }

    [Test]
    [MethodDataSource(nameof(Personnummer1890S))]
    public async Task PersonalId_1890sFixture_AllValid(string pin)
    {
        await Assert.That(PersonalId.IsValid(pin)).IsTrue();
    }

    // ── Personnummer extended (with full metadata) ──

    public static IEnumerable<(string Pin, bool Female, bool Male, bool Adult, bool Child, int Age, bool Valid)>
        PersonnummerFinal()
    {
        var rows = CsvLoader.Rows(Fixtures.PersonnummerFinal).Select(row => row.Cells);
        foreach (var r in rows)
            yield return (
                r[0],
                bool.Parse(r[1]),
                bool.Parse(r[2]),
                bool.Parse(r[3]),
                bool.Parse(r[4]),
                int.Parse(r[5], CultureInfo.InvariantCulture),
                bool.Parse(r[6]));
    }

    [Test]
    [MethodDataSource(nameof(PersonnummerFinal))]
    public async Task PersonalId_FinalFixture_MatchesAllColumns(
        string pin, bool female, bool male, bool adult, bool child, int age, bool valid)
    {
        await Assert.That(PersonalId.IsValid(pin)).IsEqualTo(valid);
        if (!valid) return;

        var id = PersonalId.Parse(pin);
        await Assert.That(id.IsFemale).IsEqualTo(female);
        await Assert.That(id.IsMale).IsEqualTo(male);
        await Assert.That(id.GetAge(PersonnummerFixtureToday)).IsEqualTo(age);
        await Assert.That(id.IsAdult(PersonnummerFixtureToday)).IsEqualTo(adult);
        await Assert.That(id.IsChild(PersonnummerFixtureToday)).IsEqualTo(child);
    }

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

    // ── Official 2000-2009 personnummer and 2020 samordningsnummer ──

    public static IEnumerable<string> Personnummer2000S()
    {
        return CsvLoader.Column(Fixtures.Personnummer2000S);
    }

    [Test]
    [MethodDataSource(nameof(Personnummer2000S))]
    public async Task PersonalId_2000sFixture_KeepsNumberAndBirthDate(string pin)
    {
        var id = PersonalId.Parse(pin);
        await Assert.That(id.LongFormat()).IsEqualTo(pin);
        await Assert.That(id.BirthDate).IsEqualTo(new OfficialPersonnummer(pin).BirthDate);
    }

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

    /// <summary>The expected cells of one organisation row, raw text as the files hold it.</summary>
    public sealed record OrganisationRow
    {
        public required string Input { get; init; }
        public required string LongFormat { get; init; }
        public required string ShortFormat { get; init; }
        public required string VatNumber { get; init; }
        public required bool Valid { get; init; }
        public required string Type { get; init; }
    }

    public static IEnumerable<OrganisationRow> OrganisationsnummerRows()
    {
        return
        [
            .. CsvLoader.Rows(Fixtures.OrganisationsnummerExtended).Select(row => row.Cells).Select(r =>
                new OrganisationRow
                {
                    Input = r[0], LongFormat = r[1], ShortFormat = r[2], Valid = bool.Parse(r[3]), Type = r[4],
                    VatNumber = r[5]
                }),
            .. CsvLoader.Rows(Fixtures.OrganisationsnummerEnskildFirma).Select(row => row.Cells).Select(r =>
                new OrganisationRow
                {
                    Input = r[0], LongFormat = r[1], ShortFormat = LastTenDigits(r[0]), Valid = bool.Parse(r[2]),
                    Type = r[3], VatNumber = $"SE{LastTenDigits(r[0])}01"
                })
        ];
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
        await Assert.That(id.ShortFormat()).IsEqualTo(row.ShortFormat);
        await Assert.That($"SE{id.ShortFormat()}01").IsEqualTo(row.VatNumber);
        // The files' "long_format" column is the short form with a separator: "-", or "+" for an Enskild firma
        // whose bearer is 100 or older. ShortFormatWithSeparator infers the same separator.
        await Assert.That(id.Format(PnrFormat.ShortFormatWithSeparator)).IsEqualTo(row.LongFormat);

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
