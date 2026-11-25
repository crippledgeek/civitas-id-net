using Civitas.Id.Sweden.Fakers.Internal;
using Civitas.Id.Sweden.Tests.Helpers;

namespace Civitas.Id.Sweden.Tests.Fakers;

public class DefectCatalogTests
{
    private const int Seed = 7;
    private const int Samples = 2000;
    private const int PersonDefects = 9;
    private const int LegalPersonDefects = 5;
    private const int LegalPersonKind = 2;
    private const int Serial = 120;
    private const int GenderDigit = 4;

    private static readonly DateOnly LeapDay = new(2000, 2, 29);

    /// <summary>1900 is divisible by four and yet no leap year.</summary>
    private static readonly DateOnly CenturyWithoutLeapDay = new(1900, 2, 28);

    private static readonly IReadOnlyList<(DefectCatalog Catalog, IReadOnlyList<IdDraft> Drafts)> Kinds =
    [
        (DefectCatalog.Personnummer, OfficialDrafts.Personnummer),
        (DefectCatalog.Samordningsnummer, OfficialDrafts.Samordningsnummer),
        (DefectCatalog.LegalPerson, OfficialDrafts.LegalPersons)
    ];

    private static IReadOnlyList<(InvalidGeneratedId Id, string Valid)> SamplesOf(int kind, IdDefect defect)
    {
        var (catalog, drafts) = Kinds[kind];
        var random = new Random(Seed);
        return
        [
            .. Enumerable.Range(0, Samples)
                .Select(i => drafts[i % drafts.Count])
                .Select(draft => (catalog.Corrupt(defect, draft, random.Next), draft.Render()))
        ];
    }

    public class Supported
    {
        [Test]
        public async Task Persons_SupportEveryDefectInOrder()
        {
            var every = Enum.GetValues<IdDefect>();

            await Assert.That(every.Length).IsEqualTo(PersonDefects);
            await Assert.That(DefectCatalog.Personnummer.Supported.SequenceEqual(every)).IsTrue();
            await Assert.That(DefectCatalog.Samordningsnummer.Supported.SequenceEqual(every)).IsTrue();
        }

        [Test]
        public async Task LegalPersons_SupportTheTextualDefectsInOrder()
        {
            IdDefect[] expected =
            [
                IdDefect.WrongCheckDigit, IdDefect.TooShort, IdDefect.TooLong, IdDefect.NonAsciiDigit,
                IdDefect.MisplacedSeparator
            ];

            await Assert.That(DefectCatalog.LegalPerson.Supported.SequenceEqual(expected)).IsTrue();
        }
    }

    public class Corrupt
    {
        [Test]
        public async Task EverySample_BreaksOnlyItsRuleAndIsRejectedByEveryParser()
        {
            var samples = Enumerable.Range(0, Kinds.Count)
                .SelectMany(kind => Kinds[kind].Catalog.Supported.SelectMany(defect => SamplesOf(kind, defect)))
                .ToList();

            await Assert.That(samples.Count).IsEqualTo(Samples * (2 * PersonDefects + LegalPersonDefects));
            await Assert.That(samples.IndexesWhere(s => !DefectInspection.BreaksOnlyItsRule(s.Id, s.Valid))).IsEmpty();
            await Assert.That(samples.IndexesWhere(s => ParserProbe.AcceptedBy(s.Id.Text).Count > 0)).IsEmpty();
        }

        [Test]
        public async Task Inspection_OfValidNumbers_FindsThemAcceptedAndBreakingNoRule()
        {
            foreach (var (catalog, drafts) in Kinds)
            {
                var text = drafts[0].Render();
                await Assert.That(ParserProbe.AcceptedBy(text)).IsNotEmpty();
                foreach (var defect in catalog.Supported)
                    await Assert.That(DefectInspection.BreaksOnlyItsRule(new InvalidGeneratedId(text, defect), text))
                        .IsFalse();
            }
        }

        [Test]
        [Arguments(0)]
        [Arguments(1)]
        public async Task FieldDefects_CoverTheirWholeRange(int kind)
        {
            var expectedDays = kind == 0
                ? DefectInspection.ImpossiblePersonalDays
                : DefectInspection.ImpossibleCoordinationDays;
            var months = SamplesOf(kind, IdDefect.ImpossibleMonth)
                .Select(s => DefectInspection.Month(s.Id.Text)).Distinct().Order();
            var days = SamplesOf(kind, IdDefect.ImpossibleDay)
                .Select(s => DefectInspection.Day(s.Id.Text)).Distinct().Order();

            await Assert.That(months.SequenceEqual(DefectInspection.ImpossibleMonths)).IsTrue();
            await Assert.That(days.SequenceEqual(expectedDays)).IsTrue();
        }

        [Test]
        [Arguments(0)]
        [Arguments(1)]
        public async Task NonLeapFebruary29_OfALeapYear_IsInAnyOfTheNextThreeYears(int kind)
        {
            var shifts = SamplesOf(kind, IdDefect.NonLeapFebruary29)
                .Where(s => DateTime.IsLeapYear(DefectInspection.FullYear(s.Valid)))
                .Select(s => DefectInspection.FullYear(s.Id.Text) - DefectInspection.FullYear(s.Valid))
                .Distinct().Order();

            await Assert.That(shifts.SequenceEqual(Enumerable.Range(1, 3))).IsTrue();
        }

        [Test]
        [Arguments(0)]
        [Arguments(1)]
        public async Task BirthYearBelowMinimum_CoversEveryCenturyAndYear(int kind)
        {
            var years = SamplesOf(kind, IdDefect.BirthYearBelowMinimum)
                .Select(s => DefectInspection.FullYear(s.Id.Text)).ToList();

            await Assert.That(years.Select(year => year / 100).Distinct().Order().SequenceEqual(Enumerable.Range(0, 18)))
                .IsTrue();
            await Assert.That(years.Select(year => year % 100).Distinct().Order().SequenceEqual(Enumerable.Range(0, 100)))
                .IsTrue();
        }

        /// <summary>The source is the lowest or the highest draw, so that both ends are reached.</summary>
        [Test]
        [Arguments(0, 28, new[] { 0, 1799 })]
        [Arguments(1, 28, new[] { 0, 1799 })]
        [Arguments(0, 29, new[] { 4, 1796 })]
        [Arguments(1, 29, new[] { 4, 1796 })]
        public async Task BirthYearBelowMinimum_ReachesTheEndsOfItsRange(int kind, int dayOfFebruary, int[] ends)
        {
            Func<DateOnly, int, int, IdDraft> draftOf =
                kind == 0 ? Generation.DraftPersonnummer : Generation.DraftSamordningsnummer;
            var draft = draftOf(new DateOnly(2000, 2, dayOfFebruary), Serial, GenderDigit);
            var catalog = Kinds[kind].Catalog;

            var lowest = catalog.Corrupt(IdDefect.BirthYearBelowMinimum, draft, (min, _) => min);
            var highest = catalog.Corrupt(IdDefect.BirthYearBelowMinimum, draft, (_, max) => max - 1);

            int[] years = [DefectInspection.FullYear(lowest.Text), DefectInspection.FullYear(highest.Text)];
            await Assert.That(years.SequenceEqual(ends)).IsTrue();
        }

        [Test]
        [Arguments(0, 7)]
        [Arguments(1, 7)]
        [Arguments(2, 5)]
        public async Task TextDefects_CoverTheirWholeRange(int kind, int lastSeparatorIndex)
        {
            var separators = SamplesOf(kind, IdDefect.MisplacedSeparator)
                .Select(s => s.Id.Text.IndexOf('-', StringComparison.Ordinal)).Distinct().Order();
            var checkDigitShifts = SamplesOf(kind, IdDefect.WrongCheckDigit)
                .Select(s => (s.Id.Text[^1] - s.Valid[^1] + 10) % 10).Distinct().Order();

            await Assert.That(separators.SequenceEqual(Enumerable.Range(0, lastSeparatorIndex + 1))).IsTrue();
            await Assert.That(checkDigitShifts.SequenceEqual(Enumerable.Range(1, 9))).IsTrue();
        }

        [Test]
        [Arguments(0)]
        [Arguments(1)]
        [Arguments(2)]
        public async Task CharacterDefects_ReachEveryPositionAndDigit(int kind)
        {
            var length = Kinds[kind].Drafts[0].Render().Length;
            var replaced = SamplesOf(kind, IdDefect.NonAsciiDigit)
                .Select(s => s.Id.Text.Index().First(c => !char.IsAscii(c.Item)).Index).Distinct().Order();
            var tooLong = SamplesOf(kind, IdDefect.TooLong);
            var insertedDigits = tooLong.Select(s => InsertedDigit(s.Id.Text, s.Valid)).Distinct().Order();
            var insertedAt = tooLong.Where(s => HasOneInsertionPosition(s.Id.Text, s.Valid))
                .Select(s => CommonPrefix(s.Id.Text, s.Valid)).Distinct().Order();
            var tooShort = SamplesOf(kind, IdDefect.TooShort);
            var removedAt = tooShort.Select(s => CommonPrefix(s.Id.Text, s.Valid)).Distinct().Order();
            var removable = tooShort.SelectMany(s => RunEnds(s.Valid)).Distinct().Order();

            await Assert.That(replaced.SequenceEqual(Enumerable.Range(0, length))).IsTrue();
            await Assert.That(insertedDigits.SequenceEqual(Enumerable.Range(0, 10))).IsTrue();
            await Assert.That(insertedAt.SequenceEqual(Enumerable.Range(0, length + 1))).IsTrue();
            await Assert.That(removedAt.SequenceEqual(removable)).IsTrue();
        }

        /// <summary>The checksum the tests judge by rejects texts of the wrong length or characters.</summary>
        [Test]
        public async Task IndependentLuhn_OfAMissingOrNonAsciiDigit_IsFalse()
        {
            IReadOnlyList<(InvalidGeneratedId Id, string Valid)> samples =
                [.. SamplesOf(LegalPersonKind, IdDefect.TooShort), .. SamplesOf(LegalPersonKind, IdDefect.NonAsciiDigit)];

            await Assert.That(samples.Count).IsEqualTo(2 * Samples);
            await Assert.That(samples.IndexesWhere(s => IndependentLuhn.IsValid(s.Id.Text))).IsEmpty();
        }

        private static int CommonPrefix(string text, string valid)
        {
            return text.Zip(valid).TakeWhile(pair => pair.First == pair.Second).Count();
        }

        private static int InsertedDigit(string text, string valid)
        {
            return text.Sum(c => c - '0') - valid.Sum(c => c - '0');
        }

        /// <summary>Inserting next to an equal digit gives the same text as inserting on its other side.</summary>
        private static bool HasOneInsertionPosition(string text, string valid)
        {
            var position = CommonPrefix(text, valid);
            return position == 0 || InsertedDigit(text, valid) != valid[position - 1] - '0';
        }

        /// <summary>Removing any of a run of equal digits gives the same text, so a removal is seen at its end.</summary>
        private static IEnumerable<int> RunEnds(string valid)
        {
            return Enumerable.Range(0, valid.Length).Where(i => i == valid.Length - 1 || valid[i] != valid[i + 1]);
        }

        [Test]
        public async Task UnsupportedOrUndefinedDefect_Throws()
        {
            var draft = OfficialDrafts.LegalPersons[0];

            await Assert.That(() =>
                    DefectCatalog.LegalPerson.Corrupt(IdDefect.ImpossibleMonth, draft, new Random(Seed).Next))
                .Throws<ArgumentOutOfRangeException>();
            await Assert.That(() =>
                    DefectCatalog.Personnummer.Corrupt((IdDefect)PersonDefects, draft, new Random(Seed).Next))
                .Throws<ArgumentOutOfRangeException>();
        }
    }

    public class Dates
    {
        [Test]
        public async Task Allows_LeapYear_ExcludesOnlyNonLeapFebruary29()
        {
            var excluded = DefectCatalog.Personnummer.Supported
                .Where(defect => !DefectCatalog.Personnummer.Allows(defect, LeapDay)).ToList();

            await Assert.That(excluded).IsEquivalentTo([IdDefect.NonLeapFebruary29]);
        }

        [Test]
        public async Task Allows_CenturyYearOrNoDate_ExcludesNothing()
        {
            var catalog = DefectCatalog.Samordningsnummer;

            await Assert.That(catalog.Supported.Count(defect => catalog.Allows(defect, CenturyWithoutLeapDay)))
                .IsEqualTo(PersonDefects);
            await Assert.That(catalog.Supported.Count(defect => catalog.Allows(defect, null)))
                .IsEqualTo(PersonDefects);
        }

        [Test]
        public async Task Allows_UnsupportedDefect_Throws()
        {
            await Assert.That(() => DefectCatalog.LegalPerson.Allows(IdDefect.ImpossibleDay, null))
                .Throws<ArgumentOutOfRangeException>();
        }

        [Test]
        public async Task Pick_YieldsEveryDefectThatGoesWithTheDate()
        {
            var random = new Random(Seed);

            var any = Enumerable.Range(0, Samples)
                .Select(_ => DefectCatalog.Personnummer.Pick(null, random.Next)).Distinct().ToList();
            var leap = Enumerable.Range(0, Samples)
                .Select(_ => DefectCatalog.Personnummer.Pick(LeapDay, random.Next)).Distinct().ToList();

            await Assert.That(any.Count).IsEqualTo(PersonDefects);
            await Assert.That(leap.Count).IsEqualTo(PersonDefects - 1);
            await Assert.That(leap).DoesNotContain(IdDefect.NonLeapFebruary29);
        }
    }
}
