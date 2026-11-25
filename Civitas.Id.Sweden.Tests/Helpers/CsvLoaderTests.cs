namespace Civitas.Id.Sweden.Tests.Helpers;

public class CsvLoaderTests
{
    private const int HeaderLines = 1;

    // The only spec used for file-integrity tests: a tiny, headerless, ASCII file.
    private static readonly FixtureSpec Small = Fixtures.InvalidIds;

    public static IEnumerable<(FixtureSpec Spec, int ExpectedRows)> ExpectedRowCounts()
    {
        // Counts are pinned independently of the specs so a spec edit cannot silently move them.
        yield return (Fixtures.Personnummer1890S, 3653);
        yield return (Fixtures.PersonnummerFinal, 3653);
        yield return (Fixtures.SamordningsnummerCorrected, 1306);
        yield return (Fixtures.OrganisationsnummerExtended, 22);
        yield return (Fixtures.OrganisationsnummerTextual, 10);
        yield return (Fixtures.InvalidIds, 9);
        yield return (Fixtures.SamordningsnummerOfficial, 1498);
        yield return (Fixtures.Personnummer2000S, 7323);
        yield return (Fixtures.Samordningsnummer2020, 14);
    }

    private const int FirstColumn = 0;
    private const int SecondColumn = 1;

    // Column indexes of testorganisationsnummer_extended.csv; a header test keeps them honest.
    private const int InputColumn = FirstColumn;
    private const int LongFormatColumn = 1;
    private const int ShortFormatColumn = 2;
    private const int VatNumberColumn = 5;

    private const int ExtendedIdColumnCount = 4;

    public static IEnumerable<(int Column, string HeaderName)> ExtendedIdColumns()
    {
        yield return (InputColumn, "input");
        yield return (LongFormatColumn, "long_format");
        yield return (ShortFormatColumn, "short_format");
        yield return (VatNumberColumn, "vat_number");
    }

    public static IEnumerable<(FixtureSpec Spec, int Column, int ExpectedDuplicates)> IdColumnDuplicates()
    {
        // Pinned from the current files. Only the extended organisation file repeats values in its
        // derived columns (long_format, short_format, vat_number); its input column is unique.
        yield return (Fixtures.Personnummer1890S, FirstColumn, 0);
        yield return (Fixtures.PersonnummerFinal, FirstColumn, 0);
        yield return (Fixtures.SamordningsnummerCorrected, FirstColumn, 0);
        yield return (Fixtures.OrganisationsnummerExtended, InputColumn, 0);
        yield return (Fixtures.OrganisationsnummerExtended, LongFormatColumn, 5);
        yield return (Fixtures.OrganisationsnummerExtended, ShortFormatColumn, 5);
        yield return (Fixtures.OrganisationsnummerExtended, VatNumberColumn, 5);
        yield return (Fixtures.OrganisationsnummerTextual, FirstColumn, 0);
        yield return (Fixtures.InvalidIds, FirstColumn, 0);
        yield return (Fixtures.SamordningsnummerOfficial, FirstColumn, 0);
        yield return (Fixtures.Personnummer2000S, FirstColumn, 0);
        yield return (Fixtures.Samordningsnummer2020, FirstColumn, 0);
    }

    // The one comparison used by every "this list covers every fixture" test: non-empty, same set as All.
    private static bool CoversExactlyAll(IEnumerable<FixtureSpec> listed)
    {
        var set = listed.ToHashSet();
        return set.Count > 0 && Fixtures.All.Count > 0 && set.SetEquals(Fixtures.All);
    }

    public class FixtureIntegrity
    {
        [Test]
        [MethodDataSource(typeof(CsvLoaderTests), nameof(ExpectedRowCounts))]
        public async Task EveryFixture_LoadsItsExactRowCount(FixtureSpec spec, int expectedRows)
        {
            var rows = CsvLoader.Rows(spec);
            await Assert.That(rows.Count).IsEqualTo(expectedRows);
        }

        [Test]
        public async Task All_IsNonEmptyWithDistinctFileNames()
        {
            await Assert.That(Fixtures.All.Count).IsGreaterThan(0);
            await Assert.That(Fixtures.All.Select(s => s.FileName).Distinct().Count()).IsEqualTo(Fixtures.All.Count);
        }

        [Test]
        public async Task ExpectedRowCountList_CoversExactlyTheSpecsInAll()
        {
            await Assert.That(CoversExactlyAll(ExpectedRowCounts().Select(e => e.Spec))).IsTrue();
        }

        [Test]
        public async Task IdColumnDuplicatesList_CoversExactlyTheSpecsInAll()
        {
            await Assert.That(CoversExactlyAll(IdColumnDuplicates().Select(e => e.Spec))).IsTrue();
        }

        [Test]
        [MethodDataSource(typeof(CsvLoaderTests), nameof(ExtendedIdColumns))]
        public async Task ExtendedIdColumnConstant_MatchesTheHeaderName(int column, string headerName)
        {
            await Assert.That(HeaderCells(Fixtures.OrganisationsnummerExtended)[column]).IsEqualTo(headerName);
        }

        [Test]
        public async Task ExtendedFile_ListsExactlyItsFourIdColumns()
        {
            var header = HeaderCells(Fixtures.OrganisationsnummerExtended);
            var listed = IdColumnDuplicates()
                .Where(e => e.Spec == Fixtures.OrganisationsnummerExtended)
                .Select(e => header[e.Column])
                .ToHashSet();
            await Assert.That(listed.Count).IsEqualTo(ExtendedIdColumnCount);
            await Assert.That(listed.SetEquals(ExtendedIdColumns().Select(c => c.HeaderName))).IsTrue();
        }

        [Test]
        public async Task FixturesOnDisk_AreExactlyTheSpecs()
        {
            // A stale file left in the output Fixtures directory (for example one deleted from the source
            // tree) fails this test; the remedy is a clean build.
            var onDisk = Directory.EnumerateFiles(FixtureFiles.Directory, "*", SearchOption.AllDirectories)
                .Select(f => Path.GetRelativePath(FixtureFiles.Directory, f))
                .ToHashSet();
            await Assert.That(onDisk.Count).IsGreaterThan(0);
            await Assert.That(onDisk.SetEquals(Fixtures.All.Select(s => s.FileName))).IsTrue();
        }

        [Test]
        [MethodDataSource(typeof(CsvLoaderTests), nameof(IdColumnDuplicates))]
        public async Task IdColumn_HasThePinnedNumberOfDuplicates(FixtureSpec spec, int column, int expectedDuplicates)
        {
            var values = CsvLoader.Column(spec, column);
            await Assert.That(values.Count - values.Distinct().Count()).IsEqualTo(expectedDuplicates);
        }
    }

    public class FailsClosed
    {
        [Test]
        public async Task MissingFile_ThrowsFileNotFound()
        {
            var missing = FixtureFiles.Missing;
            await Assert.That(() => CsvLoader.Rows(missing)).ThrowsExactly<FileNotFoundException>();
        }

        [Test]
        public async Task WrongHash_ThrowsInvalidData()
        {
            var wrong = Small with { Sha256 = Fixtures.PersonnummerFinal.Sha256 };
            await Assert.That(() => CsvLoader.Rows(wrong)).ThrowsExactly<InvalidDataException>();
        }

        [Test]
        [Arguments(1)]
        [Arguments(-1)]
        public async Task WrongRowCount_ThrowsInvalidData(int delta)
        {
            var wrong = Small with { DataRows = Small.DataRows + delta };
            await Assert.That(() => CsvLoader.Rows(wrong)).ThrowsExactly<InvalidDataException>();
        }

        [Test]
        public async Task EmptySource_ThrowsInvalidData()
        {
            var empty = Small with { Sha256 = FixtureFiles.Sha256Hex([]), DataRows = 0 };
            await AssertCopyRejected(empty, _ => []).ConfigureAwait(false);
        }

        [Test]
        public async Task ChangedByte_ThrowsInvalidData()
        {
            await AssertCopyRejected(Small, bytes =>
            {
                bytes[0] ^= 1;
                return bytes;
            }).ConfigureAwait(false);
        }

        [Test]
        public async Task FinalByteDropped_ThrowsInvalidData()
        {
            await AssertCopyRejected(Small, bytes => bytes[..^1]).ConfigureAwait(false);
        }

        [Test]
        public async Task LastLineDropped_ThrowsInvalidData()
        {
            await AssertCopyRejected(Small, DropLastLine).ConfigureAwait(false);
        }

        [Test]
        public async Task UnchangedCopy_LoadsFromAnotherDirectory()
        {
            using var dir = await TempDirectory.HoldingSmall(bytes => bytes).ConfigureAwait(false);
            var rows = CsvLoader.Rows(Small, dir.Path);
            await Assert.That(rows.Count).IsEqualTo(Small.DataRows);
        }

        [Test]
        public async Task Column_OfMissingFile_ThrowsFileNotFound()
        {
            var missing = FixtureFiles.Missing;
            await Assert.That(() => CsvLoader.Column(missing)).ThrowsExactly<FileNotFoundException>();
        }
    }

    public class RowShape
    {
        [Test]
        public async Task HeaderedFile_ExcludesHeader_AndNumbersLinesFromOne()
        {
            var rows = CsvLoader.Rows(Fixtures.PersonnummerFinal);
            await Assert.That(rows[0].Line).IsEqualTo(HeaderLines + 1);
            await Assert.That(rows[0].FileName).IsEqualTo(Fixtures.PersonnummerFinal.FileName);
            await Assert.That(rows[0].Cells.Count).IsGreaterThan(1);
        }

        [Test]
        public async Task HeaderlessFile_StartsAtLineOne()
        {
            var rows = CsvLoader.Rows(Small);
            await Assert.That(rows[0].Line).IsEqualTo(1);
        }

        [Test]
        public async Task CrlfFile_YieldsCellsWithoutCarriageReturns()
        {
            var rows = CsvLoader.Rows(Fixtures.SamordningsnummerOfficial);
            await Assert.That(rows.Any(r => r.Cells.Any(c => c.Contains('\r', StringComparison.Ordinal)))).IsFalse();
        }

        [Test]
        public async Task Column_WithIndex_ReturnsThatCellOfEveryRow()
        {
            var column = CsvLoader.Column(Fixtures.PersonnummerFinal, SecondColumn);
            var cells = CsvLoader.Rows(Fixtures.PersonnummerFinal).Select(r => r.Cells[SecondColumn]);
            await Assert.That(column.SequenceEqual(cells)).IsTrue();
        }

        [Test]
        public async Task Column_BeyondLastCell_Throws()
        {
            var tooFar = CsvLoader.Rows(Fixtures.PersonnummerFinal)[0].Cells.Count;
            await Assert.That(() => CsvLoader.Column(Fixtures.PersonnummerFinal, tooFar))
                .ThrowsExactly<ArgumentOutOfRangeException>();
        }

        [Test]
        public async Task Column_ReturnsFirstCellOfEveryRow()
        {
            var column = CsvLoader.Column(Fixtures.PersonnummerFinal);
            var firstCells = CsvLoader.Rows(Fixtures.PersonnummerFinal).Select(r => r.Cells[0]);
            await Assert.That(column.SequenceEqual(firstCells)).IsTrue();
        }
    }

    // The header line is file structure, not an ID number. Loading first proves the file is the pinned one.
    private static string[] HeaderCells(FixtureSpec spec)
    {
        _ = CsvLoader.Rows(spec);
        var header = File.ReadLines(Path.Combine(FixtureFiles.Directory, spec.FileName)).First();
        return [.. header.Split(',').Select(c => c.Trim())];
    }

    private static async Task AssertCopyRejected(FixtureSpec spec, Func<byte[], byte[]> mutate)
    {
        using var dir = await TempDirectory.HoldingSmall(mutate).ConfigureAwait(false);
        var path = dir.Path;
        await Assert.That(() => CsvLoader.Rows(spec, path)).ThrowsExactly<InvalidDataException>();
    }

    // Cuts the file back to the end of its second-to-last line, at a line boundary.
    private static byte[] DropLastLine(byte[] bytes)
    {
        var lastLineStart = Array.LastIndexOf(bytes, (byte)'\n', bytes.Length - 2) + 1;
        return bytes[..lastLineStart];
    }

    private sealed class TempDirectory : IDisposable
    {
        private TempDirectory(string path)
        {
            Path = path;
        }

        public string Path { get; }

        /// <summary>A fresh directory holding <c>Small</c>'s file, its bytes passed through <paramref name="mutate" />.</summary>
        public static async Task<TempDirectory> HoldingSmall(Func<byte[], byte[]> mutate)
        {
            var source = System.IO.Path.Combine(FixtureFiles.Directory, Small.FileName);
            var bytes = mutate(await File.ReadAllBytesAsync(source).ConfigureAwait(false));
            var dir = new TempDirectory(Directory.CreateTempSubdirectory("civitas-fixtures-").FullName);
            try
            {
                await File.WriteAllBytesAsync(System.IO.Path.Combine(dir.Path, Small.FileName), bytes)
                    .ConfigureAwait(false);
                return dir;
            }
            catch
            {
                dir.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            Directory.Delete(Path, true);
        }
    }
}
