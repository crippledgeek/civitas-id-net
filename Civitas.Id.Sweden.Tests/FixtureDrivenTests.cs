using System.Globalization;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Format;
using Civitas.Id.Sweden.Tests.Helpers;

namespace Civitas.Id.Sweden.Tests;

/// <summary>
///     Fixture-driven tests exercising the full Skatteverket-published corpus
///     across all six fixture files in <c>Fixtures/</c>.
/// </summary>
public class FixtureDrivenTests
{
    /// <summary>
    ///     Reference "today" for personnummer age assertions. Calibrated empirically
    ///     by intersecting (yyyy+age, mm, dd) bounds across every valid row of
    ///     testpersonnummer_final.csv: the unique date satisfying every row is
    ///     2025-04-23. The two Skatteverket-published CSVs unfortunately use
    ///     slightly different reference dates, so each gets its own constant.
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
        return CsvLoader.LoadColumn("Testpersonnummer 1890-1899.csv");
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
        var rows = CsvLoader.Load("testpersonnummer_final.csv").Skip(1);
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
        var rows = CsvLoader.Load("Testsamordningsnummer_2019_corrected.csv").Skip(1);
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

    // ── Organisationsnummer extended ──

    public static IEnumerable<(string Input, string LongFormatHyphenated, string ShortFormat, bool Valid, string Type)>
        OrganisationsnummerExtended()
    {
        var rows = CsvLoader.Load("testorganisationsnummer_extended.csv").Skip(1);
        foreach (var r in rows)
            yield return (
                r[0],
                r[1],
                r[2],
                bool.Parse(r[3]),
                r[4]);
    }

    [Test]
    [MethodDataSource(nameof(OrganisationsnummerExtended))]
    public async Task OrganisationId_FixtureMatchesShape(
        string input, string longFormatHyphenated, string shortFormat, bool valid, string type)
    {
        await Assert.That(OrganisationId.IsValid(input)).IsEqualTo(valid);
        if (!valid) return;

        var id = OrganisationId.Parse(input);
        await Assert.That(id.ShortFormat()).IsEqualTo(shortFormat);
        // The fixture's "long_format" column is the short-form-with-separator (e.g. "556016-0680",
        // or "121212+1212" for centenarian Enskild firma). Assert against ShortFormatWithSeparator,
        // which preserves the inferred separator ('+' for centenarians, '-' otherwise).
        await Assert.That(id.Format(PnrFormat.ShortFormatWithSeparator))
            .IsEqualTo(longFormatHyphenated);

        // The fixture's "type" column is the layperson entity name ("Aktiebolag"
        // for legal persons, "Enskild firma" for physical persons). Map and assert.
        var expectedNumberType = type switch
        {
            "Aktiebolag" => OrganisationNumberType.LegalPerson,
            "Enskild firma" => OrganisationNumberType.PhysicalPerson,
            _ => throw new InvalidOperationException($"Unknown type column value: {type}")
        };
        await Assert.That(id.NumberType).IsEqualTo(expectedNumberType);
    }

    // ── Organisationsnummer textual variants (no header, multi-format same number) ──

    public static IEnumerable<string> OrganisationsnummerTextual()
    {
        return CsvLoader.LoadColumn("testorganisationsnummer.txt");
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
        return CsvLoader.LoadColumn("invalid_swedish_ids.csv");
    }

    [Test]
    [MethodDataSource(nameof(InvalidIds))]
    public async Task InvalidIds_RejectedByAllParsers(string id)
    {
        await Assert.That(SwedishOfficialId.IsValid(id)).IsFalse();
    }
}
