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

    private static readonly RelativePath File = new("file.txt");

    // A birth date that no pinned official set covers, so no generated number can be official.
    private static readonly DateOnly BornOutsideTheSets = new(1955, 7, 14);

    // Older than 100, so the short form needs a plus sign.
    private static readonly DateOnly CentenarianOutsideTheSets = new(1905, 7, 14);

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
        return Scanner.Scan([new SourceText(File, content)]);
    }

    private static string Join(string number, string separator)
    {
        return number[..^4] + separator + number[^4..];
    }

    /// <summary>Ways a number of a bearer under 100 can be written, as functions of its 12-digit form.</summary>
    public static IEnumerable<Func<Func<string, string>>> WrittenAs()
    {
        yield return () => n => n;
        yield return () => n => n[2..];
        yield return () => n => Join(n, "-");
        yield return () => n => Join(n, "+");
        yield return () => n => Join(n[2..], "-");
        yield return () => n => Join(n[2..], " ");
        yield return () => n => Join(n[2..], "\t");
        yield return () => n => Join(n[2..], " - ");
        yield return () => n => Join(n[2..], "-\n    ");
        yield return () => n => Join(n[2..], ":");
        yield return () => n => Join(n[2..], ", ");
        yield return () => n => Join(n[2..], "\u2013");
        yield return () => n => Join(n[2..], "\u2212");
        yield return () => n => Join(n[2..], "\u200B");
        yield return () => n => Join(n[2..], "\u00AD");
        yield return () => n => Join(n[2..], "\\-");
        yield return () => n => Join(n, "\\u002B");
        yield return () => n => Join(n, "\\u{2b}");
        yield return () => n => Join(n, "\\U0000002B");
        yield return () => n => Join(n[2..], "\\x2d");
        yield return () => n => Join(n[2..], "\\u2212");
        yield return () => n => Join(n, "%2B");
        yield return () => n => Join(n, "&#43;");
        yield return () => n => Join(n[2..], "&#x2d;");
        yield return () => n => Join(n[2..], "&minus;");
        yield return () => n => "16" + n[2..];
        yield return () => n => "SE" + n;
        yield return () => n => "SE" + n[2..] + "01";
        yield return () => n => "SE" + Join(n[2..], "-") + "01";
        yield return () => n => n[2..] + "01";
        yield return () => n => $"\"{n}\"u8";
        yield return () => n => $"The number is {n}.";
        yield return () => n => $"`{n}`, **{n[2..]}**";
        yield return () => n => n + "XXX";
        yield return () => n => $"{n[..4]}-{n[4..6]}-{n[6..8]}-{n[8..]}";
        yield return () => n => $"{n[..4]}/{n[4..6]}/{n[6..8]}-{n[8..]}";
        yield return () => n => string.Join(' ', n.Chunk(2).Select(pair => new string(pair)));
        yield return () => n => string.Join('_', n[2..3], n[3..6], n[6..9], n[9..]);
        yield return () => n => string.Concat(n.Select(c => (char)(c - '0' + '\uFF10')));
        yield return () => n => string.Concat(n.Select(c => (char)(c - '0' + '\u0660')));
        yield return () => n => string.Concat(n.Select(c => $"&#{(int)c};"));
        yield return () => n => $"\"{n[..8]}\" + \"{n[8..]}\"";
        yield return () => n => $"\"{n[..5]}\"u8 +\n    \"{n[5..]}\"u8";
        yield return () => n => $"\"{n[2..8]}\" + '-' + \"{n[8..]}\"";
        yield return () => n => $"Join(\"{n[..8]}\", \"{n[8..]}\")";
        yield return () => n => $"['{string.Join("', '", n.AsEnumerable())}']";
        yield return () => n => $"/customers/{n}?x=1";
        yield return () => n => $"1234 {n} 5678";
    }

    public class Reporting
    {
        [Test]
        [MethodDataSource(typeof(PersonNumberScannerTests), nameof(WrittenAs))]
        public async Task Personnummer_InAnyWrittenForm(Func<string, string> write)
        {
            ArgumentNullException.ThrowIfNull(write);
            await AssertReported(write(UnofficialPerson().LongFormat())).ConfigureAwait(false);
        }

        [Test]
        [MethodDataSource(typeof(PersonNumberScannerTests), nameof(WrittenAs))]
        public async Task Samordningsnummer_InAnyWrittenForm(Func<string, string> write)
        {
            ArgumentNullException.ThrowIfNull(write);
            await AssertReported(write(UnofficialCoordination().LongFormat())).ConfigureAwait(false);
        }

        [Test]
        public async Task CentenarianShortForm_WithPlus()
        {
            var number = PersonalIdFaker.WithSeed(FakerSeed).Generate(CentenarianOutsideTheSets).LongFormat();
            await AssertReported(Join(number[2..], "+")).ConfigureAwait(false);
        }

        [Test]
        public async Task OfficialBody_InAnotherCentury()
        {
            await AssertReported("19" + OfficialNumbers.Centenarian.Short).ConfigureAwait(false);
        }

        /// <summary>The short forms of a centenarian that read as a younger person, and of a younger one with a plus.</summary>
        public static IEnumerable<string> ShortFormsOfAnotherCentury()
        {
            yield return OfficialNumbers.Centenarian.Short;
            yield return OfficialNumbers.Centenarian.ShortWithHyphen;
            yield return OfficialNumbers.CentenarianSamordningsnummer.Short;
            yield return OfficialNumbers.CentenarianSamordningsnummer.ShortWithHyphen;
            yield return $"SE{OfficialNumbers.Centenarian.Short}01";
            yield return $"16{OfficialNumbers.Centenarian.Short}";
            yield return OfficialNumbers.Modern.ShortWithPlus;
            yield return OfficialNumbers.TwentiethCenturySamordningsnummer.ShortWithPlus;
        }

        [Test]
        [MethodDataSource(nameof(ShortFormsOfAnotherCentury))]
        public async Task OfficialShortForm_ThatReadsAsAnotherCentury(string written)
        {
            await AssertReported(written).ConfigureAwait(false);
        }

        [Test]
        public async Task TwelveDigitsEndingIn01_WhenOnlyOneReadingIsOfficial()
        {
            // Twelve digits that read both as a number with its century and as ten digits with the VAT suffix.
            var (asWhole, asVat) = NumberWithTwoReadings();

            var onlyWhole = new PersonNumberScanner([new OfficialPersonnummer(asWhole)]);
            var onlyVat = new PersonNumberScanner([new OfficialPersonnummer(asVat)]);
            var both = new PersonNumberScanner([new OfficialPersonnummer(asWhole), new OfficialPersonnummer(asVat)]);
            SourceText[] text = [new(File, asWhole)];

            await Assert.That(onlyWhole.Scan(text).Unofficial.Count).IsEqualTo(1);
            await Assert.That(onlyVat.Scan(text).Unofficial.Count).IsEqualTo(1);
            await Assert.That(both.Scan(text).Unofficial.Count).IsEqualTo(0);
            await Assert.That(both.Scan(text).OfficialHits).IsEqualTo(1);
        }

        [Test]
        public async Task PathAndLine_WithoutTheNumber()
        {
            var number = UnofficialPerson();
            var location = Scan($"first line\nsecond line\n  {number.LongFormat()}\n").Unofficial.Single();
            await Assert.That(location.ToString()).IsEqualTo($"{File}:3");
        }

        [Test]
        public async Task LineOfTheFirstDigit_WhenLiteralsAreJoinedOverLines()
        {
            var number = UnofficialPerson().LongFormat();
            var location = Scan($"x\n\"{number[..6]}\" +\n\"{number[6..]}\"\n").Unofficial.Single();
            await Assert.That(location.ToString()).IsEqualTo($"{File}:2");
        }

        [Test]
        public async Task NumberInThePath_WithoutTheNumber()
        {
            var number = UnofficialPerson().LongFormat();
            var result = Scanner.Scan([new SourceText(new RelativePath($"a/Test_{number}.verified.txt"), "")]);
            var location = result.Unofficial.Single().ToString();
            await Assert.That(location).Contains("a/Test_############.verified.txt");
        }

        [Test]
        public async Task EveryLine_OfEveryFile()
        {
            var number = UnofficialPerson().LongFormat();
            var result = Scanner.Scan(
            [
                new SourceText(new RelativePath("a"), $"{number} {number}\n{number}"),
                new SourceText(new RelativePath("b"), number),
                new SourceText(new RelativePath("c"), "")
            ]);
            await Assert.That(result.Files.Count).IsEqualTo(3);
            await Assert.That(result.Unofficial.Select(line => line.ToString()).SequenceEqual(["a:1", "a:2", "b:1"]))
                .IsTrue();
        }

        // Searched rather than written down: the first whole number ending in 01 whose first ten digits
        // are a number. Checksum and month allow that only in the years 1901-1912.
        private static (string AsWhole, string AsVat) NumberWithTwoReadings()
        {
            const int daysPerYear = 365;
            var faker = PersonalIdFaker.WithSeed(FakerSeed);
            var firstDay = new DateOnly(1905, 1, 1);
            return Enumerable.Range(0, 1_000_000)
                .Select(i => faker.Generate(firstDay.AddDays(i % daysPerYear)).LongFormat())
                .Where(whole => whole.EndsWith("01", StringComparison.Ordinal))
                .Select(whole => (Whole: whole, Vat: NumberReading.OfShort(whole[..10], false)))
                .Where(pair => pair.Vat is not null)
                .Select(pair => (pair.Whole, pair.Vat!.Digits))
                .First();
        }

        private static async Task AssertReported(string written)
        {
            var result = Scan(written);
            await Assert.That(result.Unofficial.Count).IsGreaterThan(0);
            await Assert.That(result.OfficialHits).IsEqualTo(0);
        }
    }

    public class Accepting
    {
        /// <summary>Official numbers of bearers under 100, so that each short form has the same century.</summary>
        public static IEnumerable<Func<string>> UnderHundred()
        {
            yield return () => OfficialNumbers.Modern.Long;
            yield return () => OfficialNumbers.ModernSamordningsnummer.Long;
            yield return () => OfficialNumbers.TwentiethCenturySamordningsnummer.Long;
        }

        [Test]
        [MethodDataSource(typeof(PersonNumberScannerTests), nameof(WrittenAs))]
        public async Task OfficialNumber_InAnyWrittenForm(Func<string, string> write)
        {
            ArgumentNullException.ThrowIfNull(write);
            foreach (var number in UnderHundred())
                await Assert.That(Scan(write(number())).Unofficial.Count).IsEqualTo(0);
        }

        [Test]
        [MethodDataSource(nameof(UnderHundred))]
        public async Task OfficialNumber_IsCounted(string number)
        {
            ArgumentNullException.ThrowIfNull(number);
            await Assert.That(Scan($"{number} {number[2..]} {Join(number[2..], "-")}").OfficialHits).IsEqualTo(3);
        }

        [Test]
        public async Task CentenarianShortForm_WithPlus()
        {
            var result = Scan(
                $"{OfficialNumbers.Centenarian.ShortWithPlus} {OfficialNumbers.CentenarianSamordningsnummer.ShortWithPlus}");
            await Assert.That(result.Unofficial.Count).IsEqualTo(0);
            await Assert.That(result.OfficialHits).IsEqualTo(2);
        }

        [Test]
        public async Task OfficialNumber_WithItsDateInParts()
        {
            var number = OfficialNumbers.Centenarian.Long;
            var result = Scan($"{number[..4]}-{number[4..6]}-{number[6..8]}-{number[8..]}");
            await Assert.That(result.Unofficial.Count).IsEqualTo(0);
            await Assert.That(result.OfficialHits).IsEqualTo(1);
        }

        [Test]
        public async Task EveryOfficialNumber_IsPersonShaped()
        {
            var all = OfficialNumbers.All.ToList();
            var result = Scanner.Scan(all.Select(number => new SourceText(File, number.Long)));
            await Assert.That(result.OfficialHits).IsEqualTo(all.Count);
        }

        [Test]
        public async Task WrongCheckDigit()
        {
            var wrong = OfficialNumbers.Modern.WithWrongCheckDigit;
            await Assert.That(Scan(wrong).Unofficial.Count).IsEqualTo(0);
            await Assert.That(Scan(wrong).OfficialHits).IsEqualTo(0);
        }

        [Test]
        public async Task EveryRowOfTheInvalidFile()
        {
            var rows = CsvLoader.Column(Fixtures.InvalidIds);
            var result = Scanner.Scan(rows.Select(row => new SourceText(File, row)));
            await Assert.That(result.Files.Count).IsEqualTo(rows.Count);
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
