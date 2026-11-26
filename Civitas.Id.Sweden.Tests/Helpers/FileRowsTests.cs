namespace Civitas.Id.Sweden.Tests.Helpers;

public class FileRowsTests
{
    private static FixtureRow Row(string valid)
    {
        return new FixtureRow("file.csv", 7, ["not-a-number", "true", "false", "true", "false", "18", valid]);
    }

    [Test]
    public async Task ValidPerson_ReadsEveryColumn()
    {
        var row = FileRows.ValidPerson(Row("true"), PersonColumns.Personnummer);

        await Assert.That(row.Source).IsEqualTo("file.csv:7");
        await Assert.That(row.ToString()).IsEqualTo("file.csv:7");
        await Assert.That(row.Female).IsTrue();
        await Assert.That(row.Male).IsFalse();
        await Assert.That(row.Adult).IsTrue();
        await Assert.That(row.Child).IsFalse();
        await Assert.That(row.Age).IsEqualTo(18);
    }

    [Test]
    public async Task ValidPerson_RowThatIsNotValid_Throws()
    {
        var thrown = await Assert.That(() => FileRows.ValidPerson(Row("false"), PersonColumns.Personnummer))
            .Throws<InvalidDataException>();

        await Assert.That(thrown?.Message).Contains("file.csv:7");
    }

    [Test]
    public async Task ValidPerson_SamordningsnummerColumns_TakeAgeBeforeAdult()
    {
        FixtureRow row = new("file.csv", 2, ["not-a-number", "false", "true", "21", "true", "false", "true"]);

        var person = FileRows.ValidPerson(row, PersonColumns.Samordningsnummer);

        await Assert.That(person.Age).IsEqualTo(21);
        await Assert.That(person.Adult).IsTrue();
        await Assert.That(person.Child).IsFalse();
        await Assert.That(person.Male).IsTrue();
    }
}
