using Civitas.Id.Sweden.Tests.Helpers;

namespace Civitas.Id.Sweden.Tests.Rows;

/// <summary>Every data source of the row tests, with the file it reads and the rows it must supply.</summary>
internal static class RowSources
{
    public static readonly IReadOnlyList<RowSource> All =
    [
        Of<OfficialPersonnummerRows>(nameof(OfficialPersonnummerRows.Of1890S), Fixtures.Personnummer1890S, 3653),
        Of<OfficialPersonnummerRows>(nameof(OfficialPersonnummerRows.Of2000S), Fixtures.Personnummer2000S, 7323),
        Of<PersonnummerFinalRows>(nameof(PersonnummerFinalRows.Rows), Fixtures.PersonnummerFinal, 3653)
    ];

    private static RowSource Of<TProvider>(string method, FixtureSpec file, int rows)
    {
        return new RowSource { Provider = typeof(TProvider), Method = method, File = file, Rows = rows };
    }
}
