using Civitas.Id.Sweden.Tests.Helpers;

namespace Civitas.Id.Sweden.Tests.Rows;

/// <summary>
///     That the listed sources of the row tests read every row of every data file once, through their pins, and the
///     check that <see cref="RowDiscovery" /> makes of the discovered tests. That each source supplies its pinned rows
///     is also checked by <see cref="RowsOfAttribute{TSource, TRow}" /> on every test it feeds.
/// </summary>
public class RowSourcesTests
{
    private static readonly FixtureSpec SoleTraderFile = OrganisationRows.SoleTraders.File;

    public class Files
    {
        [Test]
        public async Task SourcesOfAFile_SupplyEveryRowOfItExactlyOnce()
        {
            await Assert.That(ListedRowSource.FilesNotSuppliedExactlyOnce(RowSources.All)).IsEmpty();
        }

        [Test]
        public async Task SourcesOfAFile_OneSourceLeftOut_NameTheFile()
        {
            var withoutDated = RowSources.All.Where(source => source.File != Fixtures.SamordningsnummerOfficial)
                .Append(ListedRowSource.Of<OfficialSamordningsnummerRows.Of2019WithoutDate, UnsupportedRow>());

            await Assert.That(ListedRowSource.FilesNotSuppliedExactlyOnce(withoutDated))
                .IsEquivalentTo([Fixtures.SamordningsnummerOfficial]);
        }

        [Test]
        public async Task SourcesOfAFile_ARowReadTwice_NameTheFile()
        {
            var twice = RowSources.All.Append(ListedRowSource.Of<OrganisationRows.SoleTraders, OrganisationRow>());

            await Assert.That(ListedRowSource.FilesNotSuppliedExactlyOnce(twice)).IsEquivalentTo([SoleTraderFile]);
        }

        [Test]
        public async Task SourcesOfAFile_ASourceDeclaringAnotherFile_NameBothFiles()
        {
            ListedRowSource[] misdeclared =
            [
                .. RowSources.All.Where(source => source.File != SoleTraderFile),
                ListedRowSource.Of<MisdeclaredSoleTraders, OrganisationRow>()
            ];

            await Assert.That(ListedRowSource.FilesNotSuppliedExactlyOnce(misdeclared))
                .IsEquivalentTo([Fixtures.OrganisationsnummerExtended]);
            await Assert.That(ListedRowSource.UnreadFiles(misdeclared)).IsEquivalentTo([SoleTraderFile]);
        }

        [Test]
        public async Task SourcesOfAFile_AListedSourceOffItsPin_Throws()
        {
            ListedRowSource[] offPin = [ListedRowSource.Of<SoleTradersPinnedAtOneMore, OrganisationRow>()];

            var thrown = await Assert.That(() => ListedRowSource.FilesNotSuppliedExactlyOnce(offPin).ToList())
                .Throws<InvalidDataException>();

            await Assert.That(thrown?.Message).Contains(nameof(SoleTradersPinnedAtOneMore));
        }

        [Test]
        public async Task Sources_ReadEveryDataFile()
        {
            await Assert.That(ListedRowSource.UnreadFiles(RowSources.All)).IsEmpty();
        }

        [Test]
        public async Task Sources_AFileUnread_NameIt()
        {
            await Assert.That(ListedRowSource.UnreadFiles(RowSources.All.Where(source => source.File != SoleTraderFile)))
                .IsEquivalentTo([SoleTraderFile]);
        }
    }

    public class DiscoveredTests
    {
        private static readonly IReadOnlyList<ListedRowSource> Listed =
        [
            ListedRowSource.Of<OrganisationRows.Textual, NumberRow>(),
            ListedRowSource.Of<OrganisationRows.SoleTraders, OrganisationRow>()
        ];

        private static readonly string TextualName = RowsOfAttribute<OrganisationRows.Textual, NumberRow>.SourceName;
        private static readonly string SoleTraderName = RowsOfAttribute<OrganisationRows.SoleTraders, OrganisationRow>.SourceName;

        /// <summary>Test methods by name; each listed source here pins two of them.</summary>
        private static readonly IReadOnlyList<string> Methods = ["Tests.First", "Tests.Second"];

        private static IEnumerable<DiscoveredRow> Taken(string name, IEnumerable<FileRow> rows, IEnumerable<string> methods)
        {
            var read = rows.ToList();
            return methods.SelectMany(method => read.Select(row => new DiscoveredRow(name, method, row.Source)));
        }

        private static IEnumerable<DiscoveredRow> AllTaken()
        {
            return Taken(TextualName, OrganisationRows.Textual.Read(), Methods)
                .Concat(Taken(SoleTraderName, OrganisationRows.SoleTraders.Read(), Methods));
        }

        [Test]
        public async Task EveryRowOfEveryListedSourceTaken_HasNoDifference()
        {
            await Assert.That(ListedRowSource.RowsNotDiscovered(Listed, AllTaken())).IsEmpty();
        }

        [Test]
        public async Task ARowNotTaken_NamesTheSourceAndTheRow()
        {
            var first = OrganisationRows.Textual.Read().First().Source;

            await Assert.That(ListedRowSource.RowsNotDiscovered(Listed, AllTaken().Where(row => row.Place != first)))
                .IsEquivalentTo([$"rows of {TextualName} that no test takes: 1, the first {first}"]);
        }

        [Test]
        public async Task ASourceThatFeedsNoTest_IsNamed()
        {
            var rows = OrganisationRows.Textual.Read().ToList();

            await Assert.That(ListedRowSource.RowsNotDiscovered(Listed, AllTaken().Where(row => row.SourceName != TextualName)))
                .IsEquivalentTo([
                    $"rows of {TextualName} that no test takes: {rows.Count}, the first {rows[0].Source}",
                    $"test methods fed by {TextualName}: 0, pinned at {OrganisationRows.Textual.Consumers}"
                ]);
        }

        [Test]
        public async Task OneConsumerFewer_NamesTheSourceAndBothCounts()
        {
            var fewer = AllTaken().Where(row => row.SourceName != TextualName || row.TestMethod == Methods[0]);

            await Assert.That(ListedRowSource.RowsNotDiscovered(Listed, fewer))
                .IsEquivalentTo([$"test methods fed by {TextualName}: 1, pinned at {OrganisationRows.Textual.Consumers}"]);
        }

        [Test]
        public async Task OneConsumerMore_NamesTheSourceAndBothCounts()
        {
            var more = AllTaken().Concat(Taken(TextualName, OrganisationRows.Textual.Read(), ["Tests.Third"]));

            await Assert.That(ListedRowSource.RowsNotDiscovered(Listed, more))
                .IsEquivalentTo([$"test methods fed by {TextualName}: 3, pinned at {OrganisationRows.Textual.Consumers}"]);
        }

        [Test]
        public async Task TheHook_CheckedThisRun_UnlessTheRunIsFiltered()
        {
            var session = TestContext.Current?.ClassContext.AssemblyContext.TestSessionContext;

            await Assert.That(RowDiscovery.Checked || session?.TestFilter is not null).IsTrue();
        }

        [Test]
        public async Task ASourceThatFeedsTestsButIsNotListed_IsNamed()
        {
            ListedRowSource[] textualOnly = [Listed[0]];

            await Assert.That(ListedRowSource.RowsNotDiscovered(textualOnly, AllTaken()))
                .IsEquivalentTo([$"{SoleTraderName} feeds tests but is not listed"]);
        }
    }

    /// <summary>The sole-trader rows, declared as rows of the extended file.</summary>
    private abstract class MisdeclaredSoleTraders : IRowSource<OrganisationRow>
    {
        public static FixtureSpec File => Fixtures.OrganisationsnummerExtended;
        public static int Pinned => OrganisationRows.SoleTraders.Pinned;
        public static int Consumers => 0;

        public static IEnumerable<OrganisationRow> Read()
        {
            return OrganisationRows.SoleTraders.Read();
        }
    }

    /// <summary>The sole-trader rows, pinned at one more.</summary>
    private abstract class SoleTradersPinnedAtOneMore : IRowSource<OrganisationRow>
    {
        public static FixtureSpec File => SoleTraderFile;
        public static int Pinned => OrganisationRows.SoleTraders.Pinned + 1;
        public static int Consumers => 0;

        public static IEnumerable<OrganisationRow> Read()
        {
            return OrganisationRows.SoleTraders.Read();
        }
    }
}
