using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Tests.Helpers;

namespace Civitas.Id.Sweden.Tests.Rows;

/// <summary>
///     One test per number of Skatteverket's personnummer files for 1890-1899 and 2000-2009 (see
///     THIRD-PARTY-DATA.md), each taken by <see cref="PersonalId" /> and rejected by <see cref="CoordinationId" />.
/// </summary>
public class OfficialPersonnummerRows
{
    public static IEnumerable<NumberRow> Of1890S()
    {
        return FileRows.Numbers(Fixtures.Personnummer1890S);
    }

    public static IEnumerable<NumberRow> Of2000S()
    {
        return FileRows.Numbers(Fixtures.Personnummer2000S);
    }

    [Test]
    [MethodDataSource(nameof(Of1890S))]
    [MethodDataSource(nameof(Of2000S))]
    public async Task Accepted_AsPersonnummer(NumberRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        await AcceptedPerson.PersonnummerAsync(new OfficialPersonnummer(row.Number), ReferenceDates.Undated).ConfigureAwait(false);
    }

    [Test]
    [MethodDataSource(nameof(Of1890S))]
    [MethodDataSource(nameof(Of2000S))]
    public async Task Rejected_AsSamordningsnummer(NumberRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        await Assert.That(ExpectedRejection.NotRejected(row.Number, ExpectedRejection.InvalidFormatFor<CoordinationId>()))
            .IsEmpty();
    }
}
