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

    /// <summary>The valid rows of the extended organisation file.</summary>
    public static IEnumerable<OrganisationRow> ValidExtendedOrganisations()
    {
        return ExtendedOrganisations(valid: true);
    }

    /// <summary>The rows of the extended organisation file that are not valid.</summary>
    public static IEnumerable<OrganisationRow> InvalidExtendedOrganisations()
    {
        return ExtendedOrganisations(valid: false);
    }

    /// <summary>
    ///     The rows of the sole-trader file, which has no short-format column: the short form is the last ten digits
    ///     of the input.
    /// </summary>
    /// <exception cref="InvalidDataException">A row is not valid: it needs a negative test.</exception>
    public static IEnumerable<OrganisationRow> SoleTraders()
    {
        return CsvLoader.Rows(Fixtures.OrganisationsnummerEnskildFirma).Select(row =>
        {
            RequireValid(row, OrganisationColumns.SoleTrader.Valid);
            return Organisation(row, OrganisationColumns.SoleTrader);
        });
    }

    /// <summary>The rows of the invalid number file, each with the reason and checksum note the tests pin for its line.</summary>
    public static IEnumerable<InvalidNumberRow> InvalidNumbers()
    {
        return CsvLoader.Rows(Fixtures.InvalidIds).Select(InvalidNumberExpectations.Of);
    }

    private static IEnumerable<OrganisationRow> ExtendedOrganisations(bool valid)
    {
        return CsvLoader.Rows(Fixtures.OrganisationsnummerExtended)
            .Where(row => bool.Parse(row.Cells[OrganisationColumns.Extended.Valid]) == valid)
            .Select(row => Organisation(row, OrganisationColumns.Extended));
    }

    private static OrganisationRow Organisation(FixtureRow row, OrganisationColumns columns)
    {
        return new OrganisationRow
        {
            Source = row.Source,
            Input = row.Cells[0],
            LongFormat = row.Cells[1],
            ShortFormat = columns.Short is { } shortColumn
                ? row.Cells[shortColumn]
                : WrittenNumber.LastTenDigits(row.Cells[0]),
            Type = row.Cells[columns.Type],
            VatNumber = columns.Vat is { } vatColumn ? row.Cells[vatColumn] : null
        };
    }

    private static void RequireValid(FixtureRow row, int validColumn)
    {
        if (!bool.Parse(row.Cells[validColumn]))
            throw new InvalidDataException($"{row.Source}: a row that is not valid needs a negative test.");
    }
}
