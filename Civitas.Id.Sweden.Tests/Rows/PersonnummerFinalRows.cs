using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Tests.Helpers;

namespace Civitas.Id.Sweden.Tests.Rows;

/// <summary>
///     One test per row of <c>testpersonnummer_final.csv</c>: the 1890-1899 numbers with the project's columns for
///     gender, age, adult and child on <see cref="ReferenceDates.PersonnummerFinal" />. Every row is valid, so the
///     file has no negative test; a row that is not valid makes the source throw.
/// </summary>
public class PersonnummerFinalRows
{
    public static IEnumerable<PersonRow> Rows()
    {
        return FileRows.ValidPersons(Fixtures.PersonnummerFinal, PersonColumns.Personnummer);
    }

    [Test]
    [MethodDataSource(nameof(Rows))]
    public async Task Accepted_WithEveryColumn(PersonRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        var reference = ReferenceDates.PersonnummerFinal;
        PhysicalPersonId id = await AcceptedPerson.PersonnummerAsync(new OfficialPersonnummer(row.Number), reference)
            .ConfigureAwait(false);

        await Assert.That(id.IsFemale).IsEqualTo(row.Female);
        await Assert.That(id.IsMale).IsEqualTo(row.Male);
        await Assert.That(id.GetAge(reference)).IsEqualTo(row.Age);
        await Assert.That(id.IsAdult(reference)).IsEqualTo(row.Adult);
        await Assert.That(id.IsChild(reference)).IsEqualTo(row.Child);
    }

    [Test]
    public async Task Numbers_AreThoseOfTheSourceFileInOrder()
    {
        var source = OfficialPersonnummerRows.Of1890S().Select(row => row.Number);
        var derived = Rows().Select(row => row.Number);
        await Assert.That(source.SequenceEqual(derived)).IsTrue();
    }
}
