using Civitas.Id.Sweden.Tests.Helpers;

namespace Civitas.Id.Sweden.Tests.Rows;

/// <summary>
///     Every source of the row tests. <see cref="RowDiscovery" /> fails a run in which a source feeds tests but is not
///     listed here.
/// </summary>
internal static class RowSources
{
    public static readonly IReadOnlyList<ListedRowSource> All =
    [
        ListedRowSource.Of<OfficialPersonnummerRows.Of1890S, NumberRow>(),
        ListedRowSource.Of<OfficialPersonnummerRows.Of2000S, NumberRow>(),
        ListedRowSource.Of<PersonnummerFinalRows.All, PersonRow>(),
        ListedRowSource.Of<OfficialSamordningsnummerRows.Of2019WithDate, NumberRow>(),
        ListedRowSource.Of<OfficialSamordningsnummerRows.Of2019WithoutDate, UnsupportedRow>(),
        ListedRowSource.Of<OfficialSamordningsnummerRows.Of2020, NumberRow>(),
        ListedRowSource.Of<SamordningsnummerCorrectedRows.All, PersonRow>(),
        ListedRowSource.Of<OrganisationRows.ExtendedValid, OrganisationRow>(),
        ListedRowSource.Of<OrganisationRows.ExtendedInvalid, OrganisationRow>(),
        ListedRowSource.Of<OrganisationRows.SoleTraders, OrganisationRow>(),
        ListedRowSource.Of<OrganisationRows.Textual, NumberRow>(),
        ListedRowSource.Of<InvalidNumberRows.All, InvalidNumberRow>()
    ];
}
