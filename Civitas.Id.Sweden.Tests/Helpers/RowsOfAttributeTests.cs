using TUnit.Core.Enums;

namespace Civitas.Id.Sweden.Tests.Helpers;

public class RowsOfAttributeTests
{
    private static readonly FixtureSpec TextualFile = Fixtures.OrganisationsnummerTextual;

    [Test]
    public async Task Rows_SourceSuppliesItsPin_AreTheRowsItReads()
    {
        var rows = RowsOfAttribute<RightPin, NumberRow>.Rows();

        await Assert.That(rows.Count).IsEqualTo(TextualFile.DataRows);
        await Assert.That(rows.SequenceEqual(FileRows.Numbers(TextualFile))).IsTrue();
    }

    [Test]
    public async Task Rows_SourceSuppliesFewerThanItsPin_ThrowsNamingTheSourceAndBothCounts()
    {
        var thrown = await Assert.That(RowsOfAttribute<PinnedAtOneMore, NumberRow>.Rows).Throws<InvalidDataException>();

        await AssertNamesAsync(thrown, nameof(PinnedAtOneMore), TextualFile.DataRows + 1).ConfigureAwait(false);
    }

    [Test]
    public async Task Rows_SourceSuppliesMoreThanItsPin_ThrowsNamingTheSourceAndBothCounts()
    {
        var thrown = await Assert.That(RowsOfAttribute<PinnedAtOneFewer, NumberRow>.Rows).Throws<InvalidDataException>();

        await AssertNamesAsync(thrown, nameof(PinnedAtOneFewer), TextualFile.DataRows - 1).ConfigureAwait(false);
    }

    [Test]
    public async Task Rows_EmptySourcePinnedAtNothing_Throws()
    {
        var thrown = await Assert.That(RowsOfAttribute<NoPin, NumberRow>.Rows).Throws<InvalidDataException>();

        await Assert.That(thrown?.Message).Contains($"{nameof(RowsOfAttributeTests)}.{nameof(NoPin)}");
    }

    [Test]
    public async Task DataRows_SourceSuppliesItsPin_AreTheRowsItReads()
    {
        var rows = await DataRowsAsync(new RowsOfAttribute<RightPin, NumberRow>()).ConfigureAwait(false);

        await Assert.That(rows.SequenceEqual(FileRows.Numbers(TextualFile))).IsTrue();
    }

    [Test]
    public async Task DataRows_SourceOffItsPin_ThrowsNamingTheSourceAndBothCounts()
    {
        var thrown = await Assert.That(async () => { _ = await DataRowsAsync(new RowsOfAttribute<PinnedAtOneMore, NumberRow>()).ConfigureAwait(false); })
            .Throws<InvalidDataException>();

        await AssertNamesAsync(thrown, nameof(PinnedAtOneMore), TextualFile.DataRows + 1).ConfigureAwait(false);
    }

    [Test]
    public async Task SourceName_IsTheSourceAfterTheTypeItIsNestedIn()
    {
        INamedRowSource attribute = new RowsOfAttribute<RightPin, NumberRow>();

        await Assert.That(attribute.SourceName).IsEqualTo($"{nameof(RowsOfAttributeTests)}.{nameof(RightPin)}");
    }

    /// <summary>The rows the attribute gives TUnit, through the entry point TUnit calls for a test's data.</summary>
    private static async Task<IReadOnlyList<NumberRow>> DataRowsAsync<TSource>(RowsOfAttribute<TSource, NumberRow> attribute)
        where TSource : IRowSource<NumberRow>
    {
        var test = TestContext.Current ?? throw new InvalidOperationException("No test is running.");
        var metadata = new DataGeneratorMetadata
        {
            TestBuilderContext = new TestBuilderContextAccessor(new TestBuilderContext { TestMetadata = test.Metadata.TestDetails.MethodMetadata }),
            MembersToGenerate = [],
            TestInformation = test.Metadata.TestDetails.MethodMetadata,
            Type = DataGeneratorType.TestParameters,
            TestSessionId = test.ClassContext.AssemblyContext.TestSessionContext.Id,
            TestClassInstance = null,
            ClassInstanceArguments = null
        };

        List<NumberRow> rows = [];
        await foreach (var row in attribute.GetTypedDataRowsAsync(metadata).ConfigureAwait(false))
        {
            rows.Add(await row().ConfigureAwait(false));
        }

        return rows;
    }

    private static async Task AssertNamesAsync(InvalidDataException? thrown, string source, int pinned)
    {
        await Assert.That(thrown?.Message).Contains($"{nameof(RowsOfAttributeTests)}.{source} ");
        await Assert.That(thrown?.Message).Contains($" supplies {TextualFile.DataRows} rows of {TextualFile.FileName}");
        await Assert.That(thrown?.Message).Contains($"pinned at {pinned}.");
    }

    /// <summary>The file's rows, pinned at their number.</summary>
    private abstract class RightPin : IRowSource<NumberRow>
    {
        public static FixtureSpec File => TextualFile;
        public static int Pinned => File.DataRows;
        public static int Consumers => 0;

        public static IEnumerable<NumberRow> Read()
        {
            return FileRows.Numbers(File);
        }
    }

    /// <summary>The file's rows, pinned at one more.</summary>
    private abstract class PinnedAtOneMore : IRowSource<NumberRow>
    {
        public static FixtureSpec File => TextualFile;
        public static int Pinned => File.DataRows + 1;
        public static int Consumers => 0;

        public static IEnumerable<NumberRow> Read()
        {
            return FileRows.Numbers(File);
        }
    }

    /// <summary>The file's rows, pinned at one fewer.</summary>
    private abstract class PinnedAtOneFewer : IRowSource<NumberRow>
    {
        public static FixtureSpec File => TextualFile;
        public static int Pinned => File.DataRows - 1;
        public static int Consumers => 0;

        public static IEnumerable<NumberRow> Read()
        {
            return FileRows.Numbers(File);
        }
    }

    /// <summary>No rows, pinned at none: a test fed by it would not run.</summary>
    private abstract class NoPin : IRowSource<NumberRow>
    {
        public static FixtureSpec File => TextualFile;
        public static int Pinned => 0;
        public static int Consumers => 0;

        public static IEnumerable<NumberRow> Read()
        {
            return [];
        }
    }
}
