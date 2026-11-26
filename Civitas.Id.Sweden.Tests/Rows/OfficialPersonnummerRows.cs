using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Tests.Helpers;

namespace Civitas.Id.Sweden.Tests.Rows;

/// <summary>
///     One test per number of Skatteverket's personnummer files for 1890-1899 and 2000-2009 (see
///     THIRD-PARTY-DATA.md), each taken by <see cref="PersonalId" /> and rejected by <see cref="CoordinationId" />.
/// </summary>
public class OfficialPersonnummerRows
{
    /// <summary>The numbers of the 1890-1899 file.</summary>
    public abstract class Of1890S : IRowSource<NumberRow>
    {
        public static FixtureSpec File => Fixtures.Personnummer1890S;
        public static int Pinned => 3653;
        public static int Consumers => 2;

        public static IEnumerable<NumberRow> Read()
        {
            return FileRows.Numbers(File);
        }
    }

    /// <summary>The numbers of the 2000-2009 file.</summary>
    public abstract class Of2000S : IRowSource<NumberRow>
    {
        public static FixtureSpec File => Fixtures.Personnummer2000S;
        public static int Pinned => 7323;
        public static int Consumers => 2;

        public static IEnumerable<NumberRow> Read()
        {
            return FileRows.Numbers(File);
        }
    }

    [Test]
    [RowsOf<Of1890S, NumberRow>]
    [RowsOf<Of2000S, NumberRow>]
    public async Task Accepted_AsPersonnummer(NumberRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        await AcceptedPerson.PersonnummerAsync(new OfficialPersonnummer(row.Number), ReferenceDates.Undated).ConfigureAwait(false);
    }

    [Test]
    [RowsOf<Of1890S, NumberRow>]
    [RowsOf<Of2000S, NumberRow>]
    public async Task Rejected_AsSamordningsnummer(NumberRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        await Assert.That(ExpectedRejection.NotRejected(row.Number, ExpectedRejection.InvalidFormatFor<CoordinationId>()))
            .IsEmpty();
    }
}
