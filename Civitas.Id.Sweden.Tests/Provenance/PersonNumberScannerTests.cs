using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Fakers;
using Civitas.Id.Sweden.Tests.Helpers;

namespace Civitas.Id.Sweden.Tests.Provenance;

/// <summary>
///     The scanned text is built at run time from the Fakers and the pinned files, so this file holds no number that
///     the scanner itself would report.
/// </summary>
public class PersonNumberScannerTests
{
    private const int FakerSeed = 1;
    private const string FileName = "file.txt";

    // A birth year that no pinned official set covers, so no generated number can be official.
    private static readonly DateOnly BornOutsideTheSets = new(1955, 7, 14);

    private static readonly PersonNumberScanner Scanner = new(OfficialNumbers.All);

    private static PersonalId UnofficialPerson()
    {
        return PersonalIdFaker.WithSeed(FakerSeed).Generate(BornOutsideTheSets);
    }

    private static CoordinationId UnofficialCoordination()
    {
        return CoordinationIdFaker.WithSeed(FakerSeed).Generate(BornOutsideTheSets);
    }

    private static ScanResult Scan(string content)
    {
        return Scanner.Scan([new SourceText(FileName, content)]);
    }

    private static string Join(string number, string separator)
    {
        return number[..^4] + separator + number[^4..];
    }

    public class Reporting
    {
        /// <summary>Ways a number can be written, as functions of its 12-digit form.</summary>
        public static IEnumerable<Func<Func<string, string>>> WrittenAs()
        {
            yield return () => n => n;
            yield return () => n => n[2..];
            yield return () => n => Join(n, "-");
            yield return () => n => Join(n, "+");
            yield return () => n => Join(n[2..], "-");
            yield return () => n => Join(n[2..], "+");
            yield return () => n => Join(n[2..], " ");
            yield return () => n => Join(n[2..], "–");
            yield return () => n => Join(n[2..], @"+");
            yield return () => n => Join(n, "%2B");
            yield return () => n => "16" + n[2..];
            yield return () => n => "SE" + n;
            yield return () => n => "SE" + n[2..] + "01";
            yield return () => n => "SE" + Join(n[2..], "-") + "01";
            yield return () => n => n[2..] + "01";
            yield return () => n => $"\"{n}\"u8";
            yield return () => n => $"The number is {n}.";
            yield return () => n => $"`{n}`, **{n[2..]}**";
            yield return () => n => n + "XXX";
            yield return () => n => $"{n[..8]}\" + \"{n[8..]}";
            yield return () => n => $"{n[..5]}\"u8 +\n    \"{n[5..]}";
            yield return () => n => $"/customers/{n}?x=1";
        }

        [Test]
        [MethodDataSource(nameof(WrittenAs))]
        public async Task Personnummer_InAnyWrittenForm(Func<string, string> write)
        {
            ArgumentNullException.ThrowIfNull(write);
            var result = Scan(write(UnofficialPerson().LongFormat()));
            await Assert.That(result.Unofficial.Count).IsGreaterThan(0);
            await Assert.That(result.OfficialHits).IsEqualTo(0);
        }

        [Test]
        [MethodDataSource(nameof(WrittenAs))]
        public async Task Samordningsnummer_InAnyWrittenForm(Func<string, string> write)
        {
            ArgumentNullException.ThrowIfNull(write);
            var result = Scan(write(UnofficialCoordination().LongFormat()));
            await Assert.That(result.Unofficial.Count).IsGreaterThan(0);
            await Assert.That(result.OfficialHits).IsEqualTo(0);
        }

        [Test]
        public async Task OfficialBody_InAnotherCentury()
        {
            var otherCentury = "19" + OfficialNumbers.Centenarian.Short;
            await Assert.That(Scan(otherCentury).Unofficial.Count).IsEqualTo(1);
        }

        [Test]
        public async Task PathAndLine_WithoutTheNumber()
        {
            var number = UnofficialPerson();
            var location = Scan($"first line\nsecond line\n  {number.LongFormat()}\n").Unofficial.Single().ToString();
            await Assert.That(location).IsEqualTo($"{FileName}:3");
        }

        [Test]
        public async Task EveryNumber_OfEveryFile()
        {
            var number = UnofficialPerson().LongFormat();
            var result = Scanner.Scan(
                [new SourceText("a", $"{number} {number}"), new SourceText("b", number), new SourceText("c", "")]);
            await Assert.That(result.FilesScanned).IsEqualTo(3);
            await Assert.That(result.Unofficial.Select(line => line.ToString()).SequenceEqual(["a:1", "a:1", "b:1"]))
                .IsTrue();
        }
    }

    public class Accepts
    {
        [Test]
        [MethodDataSource(typeof(Reporting), nameof(Reporting.WrittenAs))]
        public async Task OfficialNumber_InAnyWrittenForm(Func<string, string> write)
        {
            ArgumentNullException.ThrowIfNull(write);
            foreach (var number in new OfficialNumber[]
                         { OfficialNumbers.Centenarian, OfficialNumbers.ModernSamordningsnummer })
            {
                var result = Scan(write(number.Long));
                await Assert.That(result.Unofficial.Count).IsEqualTo(0);
                await Assert.That(result.OfficialHits).IsGreaterThan(0);
            }
        }

        [Test]
        public async Task EveryOfficialNumber_IsPersonShaped()
        {
            var all = OfficialNumbers.All.ToList();
            var result = Scanner.Scan(all.Select(number => new SourceText(FileName, number.Long)));
            await Assert.That(result.OfficialHits).IsEqualTo(all.Count);
        }

        [Test]
        public async Task WrongCheckDigit()
        {
            var number = UnofficialPerson().LongFormat();
            var wrong = number[..^1] + (char)('0' + (number[^1] - '0' + 1) % 10);
            await Assert.That(Scan(wrong).Unofficial.Count).IsEqualTo(0);
        }

        [Test]
        public async Task EveryRowOfTheInvalidFile()
        {
            var rows = CsvLoader.Column(Fixtures.InvalidIds);
            var result = Scanner.Scan(rows.Select(row => new SourceText(FileName, row)));
            await Assert.That(result.FilesScanned).IsEqualTo(rows.Count);
            await Assert.That(result.Unofficial.Count).IsEqualTo(0);
        }

        [Test]
        public async Task LegalPersonNumber()
        {
            var legal = OrganisationIdFaker.WithSeed(FakerSeed).GenerateLegalPerson().LongFormat();
            var result = Scan($"{legal} SE{legal}01 16{legal}");
            await Assert.That(result.Unofficial.Count).IsEqualTo(0);
        }

        [Test]
        public async Task NumberInsideALongerRunOfDigits()
        {
            var number = UnofficialPerson().LongFormat();
            await Assert.That(Scan($"9{number}99").Unofficial.Count).IsEqualTo(0);
        }
    }
}
