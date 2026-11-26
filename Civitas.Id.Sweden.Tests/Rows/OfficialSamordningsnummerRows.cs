using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Tests.Helpers;

namespace Civitas.Id.Sweden.Tests.Rows;

/// <summary>
///     One test per number of Skatteverket's samordningsnummer files for 2019 and 2020 (see THIRD-PARTY-DATA.md). The
///     numbers with a calendar date are taken by <see cref="CoordinationId" /> and rejected by
///     <see cref="PersonalId" />. The others have no calendar date, and no parser takes them yet.
/// </summary>
public class OfficialSamordningsnummerRows
{
    /// <summary>The numbers of the 2019 file that have a calendar date.</summary>
    public abstract class Of2019WithDate : IRowSource<NumberRow>
    {
        public static FixtureSpec File => Fixtures.SamordningsnummerOfficial;
        public static int Pinned => 1306;
        public static int Consumers => 2;

        public static IEnumerable<NumberRow> Read()
        {
            return FileRows.Numbers(File)
                .Where(row => UnsupportedShapes.Of(new OfficialSamordningsnummer(row.Number)) is null);
        }
    }

    /// <summary>The numbers of the 2019 file that have no calendar date.</summary>
    public abstract class Of2019WithoutDate : IRowSource<UnsupportedRow>
    {
        public static FixtureSpec File => Fixtures.SamordningsnummerOfficial;
        public static int Pinned => 192;
        public static int Consumers => 1;

        public static IEnumerable<UnsupportedRow> Read()
        {
            return CsvLoader.Rows(File)
                .Select(row => (row, shape: UnsupportedShapes.Of(new OfficialSamordningsnummer(row.Cells[0]))))
                .Where(read => read.shape is not null)
                .Select(read => new UnsupportedRow { Source = read.row.Source, Number = read.row.Cells[0], Shape = read.shape!.Value });
        }
    }

    /// <summary>The numbers of the 2020 file.</summary>
    public abstract class Of2020 : IRowSource<NumberRow>
    {
        public static FixtureSpec File => Fixtures.Samordningsnummer2020;
        public static int Pinned => 14;
        public static int Consumers => 2;

        public static IEnumerable<NumberRow> Read()
        {
            return FileRows.Numbers(File);
        }
    }

    [Test]
    [RowsOf<Of2019WithDate, NumberRow>]
    [RowsOf<Of2020, NumberRow>]
    public async Task Accepted_AsSamordningsnummer(NumberRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        await AcceptedPerson.SamordningsnummerAsync(new OfficialSamordningsnummer(row.Number), ReferenceDates.Undated)
            .ConfigureAwait(false);
    }

    [Test]
    [RowsOf<Of2019WithDate, NumberRow>]
    [RowsOf<Of2020, NumberRow>]
    public async Task Rejected_AsPersonnummer(NumberRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        await Assert.That(ExpectedRejection.NotRejected(row.Number, ExpectedRejection.InvalidFormatFor<PersonalId>()))
            .IsEmpty();
    }

    /// <summary>
    ///     Fails for a row as soon as any parser takes it: the day it does, the row belongs with the accepted ones.
    /// </summary>
    [Test]
    [RowsOf<Of2019WithoutDate, UnsupportedRow>]
    public async Task KnownUnsupported_OfficialSamordningsnummer_RejectedByEveryParserToday(UnsupportedRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        await Assert.That(ExpectedRejection.NotRejected(row.Number, ExpectedRejection.ByEveryType)).IsEmpty();
    }

    [Test]
    public async Task KnownUnsupported_OfficialSamordningsnummer_HasThePinnedShapes()
    {
        var counts = Of2019WithoutDate.Read().GroupBy(row => row.Shape).ToDictionary(group => group.Key, group => group.Count());

        await Assert.That(counts.GetValueOrDefault(UnsupportedShape.MonthUnknown)).IsEqualTo(127);
        await Assert.That(counts.GetValueOrDefault(UnsupportedShape.DayUnknown)).IsEqualTo(38);
        await Assert.That(counts.GetValueOrDefault(UnsupportedShape.MonthAndDayUnknown)).IsEqualTo(3);
        await Assert.That(counts.GetValueOrDefault(UnsupportedShape.ImpossibleDate)).IsEqualTo(24);
        await Assert.That(counts.Count).IsEqualTo(Enum.GetValues<UnsupportedShape>().Length);
    }
}
