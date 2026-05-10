namespace Civitas.Id.Sweden.Tests.Helpers;

public class CsvLoaderTests
{
    public class FixtureIntegrity
    {
        // Exact row counts locked in to detect any future loader regression
        // (silent drops, BOM corruption, encoding issues, etc.). If a fixture
        // file is updated, these expectations must move with it.

        [Test]
        public async Task Personnummer1890s_HasExactly3653Rows()
        {
            var rows = CsvLoader.LoadColumn("Testpersonnummer 1890-1899.csv");
            await Assert.That(rows.Count).IsEqualTo(3653);
        }

        [Test]
        public async Task PersonnummerFinal_HasExactly3654Rows_OneHeaderPlus3653Data()
        {
            var rows = CsvLoader.Load("testpersonnummer_final.csv");
            await Assert.That(rows.Count).IsEqualTo(3654);
            await Assert.That(rows[0][0]).IsEqualTo("Testpersonnummer");
        }

        [Test]
        public async Task Samordningsnummer_HasExactly1307Rows_OneHeaderPlus1306Data()
        {
            var rows = CsvLoader.Load("Testsamordningsnummer_2019_corrected.csv");
            await Assert.That(rows.Count).IsEqualTo(1307);
            await Assert.That(rows[0][0]).IsEqualTo("Testpersonnummer");
        }

        [Test]
        public async Task OrganisationsnummerTextual_HasExactly10Rows()
        {
            var rows = CsvLoader.LoadColumn("testorganisationsnummer.txt");
            await Assert.That(rows.Count).IsEqualTo(10);
        }

        [Test]
        public async Task OrganisationsnummerExtended_HasExactly23Rows_OneHeaderPlus22Data()
        {
            var rows = CsvLoader.Load("testorganisationsnummer_extended.csv");
            await Assert.That(rows.Count).IsEqualTo(23);
            await Assert.That(rows[0][0]).IsEqualTo("input");
        }

        [Test]
        public async Task InvalidIds_HasExactly9Rows()
        {
            var rows = CsvLoader.LoadColumn("invalid_swedish_ids.csv");
            await Assert.That(rows.Count).IsEqualTo(9);
        }
    }

    public class LoadColumn
    {
        [Test]
        public async Task Personnummer1890s_FirstRow_IsExpected()
        {
            var rows = CsvLoader.LoadColumn("Testpersonnummer 1890-1899.csv");
            await Assert.That(rows[0]).IsEqualTo("189001019802");
        }

        [Test]
        public async Task Personnummer1890s_LastRow_IsExpected()
        {
            var rows = CsvLoader.LoadColumn("Testpersonnummer 1890-1899.csv");
            // Trailing-partial-line check: the file has no final newline; the
            // last row must be present and well-formed.
            await Assert.That(rows[^1].Length).IsEqualTo(12);
            await Assert.That(rows[^1].StartsWith("18", StringComparison.Ordinal)).IsTrue();
        }
    }
}
