using Civitas.Id.Sweden.Tests.Helpers;

namespace Civitas.Id.Sweden.Tests.Rows;

/// <summary>Every data source of the row tests, with the file it reads and the rows it must supply.</summary>
internal static class RowSources
{
    public static readonly IReadOnlyList<RowSource> All =
    [
        Of<OfficialPersonnummerRows>(nameof(OfficialPersonnummerRows.Of1890S), Fixtures.Personnummer1890S, 3653),
        Of<OfficialPersonnummerRows>(nameof(OfficialPersonnummerRows.Of2000S), Fixtures.Personnummer2000S, 7323),
        Of<PersonnummerFinalRows>(nameof(PersonnummerFinalRows.Rows), Fixtures.PersonnummerFinal, 3653),
        Of<OfficialSamordningsnummerRows>(nameof(OfficialSamordningsnummerRows.Of2019WithDate),
            Fixtures.SamordningsnummerOfficial, 1306),
        Of<OfficialSamordningsnummerRows>(nameof(OfficialSamordningsnummerRows.Of2019WithoutDate),
            Fixtures.SamordningsnummerOfficial, 192),
        Of<OfficialSamordningsnummerRows>(nameof(OfficialSamordningsnummerRows.Of2020),
            Fixtures.Samordningsnummer2020, 14),
        Of<SamordningsnummerCorrectedRows>(nameof(SamordningsnummerCorrectedRows.Rows),
            Fixtures.SamordningsnummerCorrected, 1306),
        Of<OrganisationRows>(nameof(OrganisationRows.ExtendedValid), Fixtures.OrganisationsnummerExtended, 6),
        Of<OrganisationRows>(nameof(OrganisationRows.ExtendedInvalid), Fixtures.OrganisationsnummerExtended, 4),
        Of<OrganisationRows>(nameof(OrganisationRows.SoleTraders), Fixtures.OrganisationsnummerEnskildFirma, 20),
        Of<OrganisationRows>(nameof(OrganisationRows.Textual), Fixtures.OrganisationsnummerTextual, 6),
        Of<InvalidNumberRows>(nameof(InvalidNumberRows.Rows), Fixtures.InvalidIds, 9)
    ];

    private static RowSource Of<TProvider>(string method, FixtureSpec file, int rows)
    {
        return new RowSource { Provider = typeof(TProvider), Method = method, File = file, Rows = rows };
    }
}
