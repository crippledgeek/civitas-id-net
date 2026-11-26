using System.Globalization;

namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>Reads the rows of the pinned data files as test parameters, each named by its place.</summary>
internal static class FileRows
{
    /// <summary>The first cell of every row of a file.</summary>
    public static IEnumerable<NumberRow> Numbers(FixtureSpec spec)
    {
        return CsvLoader.Rows(spec).Select(row => new NumberRow { Source = row.Source, Number = row.Cells[0] });
    }

    /// <summary>
    ///     The rows of a derived person number file, all of which are valid.
    /// </summary>
    /// <exception cref="InvalidDataException">
    ///     A row is not valid: it needs a negative test, and a positive one must not be fed it.
    /// </exception>
    public static IEnumerable<PersonRow> ValidPersons(FixtureSpec spec, PersonColumns columns)
    {
        return CsvLoader.Rows(spec).Select(row => ValidPerson(row, columns));
    }

    internal static PersonRow ValidPerson(FixtureRow row, PersonColumns columns)
    {
        RequireValid(row, columns.Valid);

        return new PersonRow
        {
            Source = row.Source,
            Number = row.Cells[0],
            Female = bool.Parse(row.Cells[columns.Female]),
            Male = bool.Parse(row.Cells[columns.Male]),
            Age = int.Parse(row.Cells[columns.Age], CultureInfo.InvariantCulture),
            Adult = bool.Parse(row.Cells[columns.Adult]),
            Child = bool.Parse(row.Cells[columns.Child])
        };
    }

    private static void RequireValid(FixtureRow row, int validColumn)
    {
        if (!bool.Parse(row.Cells[validColumn]))
            throw new InvalidDataException($"{row.Source}: a row that is not valid needs a negative test.");
    }
}
