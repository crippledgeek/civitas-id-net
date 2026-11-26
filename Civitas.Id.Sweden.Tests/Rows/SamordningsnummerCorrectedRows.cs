using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Tests.Helpers;

namespace Civitas.Id.Sweden.Tests.Rows;

/// <summary>
///     One test per row of <c>Testsamordningsnummer_2019_corrected.csv</c>: the numbers of the official 2019 file that
///     have a calendar date, with the project's columns for gender, age, adult and child. The columns hold on
///     <see cref="ReferenceDates.SamordningsnummerCorrected" />, and on the last day before each row's next birthday.
///     Every row is valid, so the file has no negative test; a row that is not valid makes the source throw.
/// </summary>
public class SamordningsnummerCorrectedRows
{
    public static IEnumerable<PersonRow> Rows()
    {
        return FileRows.ValidPersons(Fixtures.SamordningsnummerCorrected, PersonColumns.Samordningsnummer);
    }

    [Test]
    [MethodDataSource(nameof(Rows))]
    public async Task Accepted_WithEveryColumn(PersonRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        var number = new OfficialSamordningsnummer(row.Number);
        PhysicalPersonId id = await AcceptedPerson.SamordningsnummerAsync(number, ReferenceDates.SamordningsnummerCorrected)
            .ConfigureAwait(false);

        await Assert.That(id.IsFemale).IsEqualTo(row.Female);
        await Assert.That(id.IsMale).IsEqualTo(row.Male);
        await AssertAgeColumnsOn(id, row, ReferenceDates.SamordningsnummerCorrected).ConfigureAwait(false);
        await AssertAgeColumnsOn(id, row, number.BirthDate.AddYears(row.Age + 1).AddDays(-1)).ConfigureAwait(false);
    }

    [Test]
    public async Task Numbers_AreTheOfficialFilesNumbersWithADateInOrder()
    {
        var official = OfficialSamordningsnummerRows.Of2019WithDate().Select(row => row.Number);
        var corrected = Rows().Select(row => row.Number);
        await Assert.That(official.SequenceEqual(corrected)).IsTrue();
    }

    private static async Task AssertAgeColumnsOn(PhysicalPersonId id, PersonRow row, DateOnly reference)
    {
        await Assert.That(id.GetAge(reference)).IsEqualTo(row.Age);
        await Assert.That(id.IsAdult(reference)).IsEqualTo(row.Adult);
        await Assert.That(id.IsChild(reference)).IsEqualTo(row.Child);
    }
}
