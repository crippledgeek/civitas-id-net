using Civitas.Id.Sweden.Fakers.Internal;
using Civitas.Id.Sweden.Tests.Helpers;

namespace Civitas.Id.Sweden.Tests.Fakers;

public class DefectCatalogTests
{
    private const int Seed = 7;
    private const int Samples = 2000;
    private const int LegalPersonDefects = 5;
    private const int SeparatorIndexes = 6;

    private static IReadOnlyList<(InvalidGeneratedId Id, string Valid)> SamplesOf(IdDefect defect)
    {
        var drafts = OfficialDrafts.LegalPersons;
        var random = new Random(Seed);
        return
        [
            .. Enumerable.Range(0, Samples)
                .Select(i => drafts[i % drafts.Count])
                .Select(draft => (DefectCatalog.LegalPerson.Corrupt(defect, draft, random.Next), draft.Render()))
        ];
    }

    public class Supported
    {
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
            var samples = DefectCatalog.LegalPerson.Supported.SelectMany(SamplesOf).ToList();

            await Assert.That(samples.Count).IsEqualTo(Samples * LegalPersonDefects);
            await Assert.That(samples.IndexesWhere(s => !DefectInspection.BreaksOnlyItsRule(s.Id, s.Valid))).IsEmpty();
            await Assert.That(samples.IndexesWhere(s => ParserProbe.AcceptedBy(s.Id.Text).Count > 0)).IsEmpty();
        }

        [Test]
        public async Task Inspection_OfValidNumbers_FindsThemAcceptedAndBreakingNoRule()
        {
            var text = OfficialDrafts.LegalPersons[0].Render();

            await Assert.That(ParserProbe.AcceptedBy(text)).IsNotEmpty();
            await Assert.That(DefectCatalog.LegalPerson.Supported.Count).IsEqualTo(LegalPersonDefects);
            foreach (var defect in DefectCatalog.LegalPerson.Supported)
                await Assert.That(DefectInspection.BreaksOnlyItsRule(new InvalidGeneratedId(text, defect), text))
                    .IsFalse();
        }

        [Test]
        public async Task TextDefects_CoverTheirWholeRange()
        {
            var separators = SamplesOf(IdDefect.MisplacedSeparator)
                .Select(s => s.Id.Text.IndexOf('-', StringComparison.Ordinal)).Distinct().Order();
            var checkDigitShifts = SamplesOf(IdDefect.WrongCheckDigit)
                .Select(s => (s.Id.Text[^1] - s.Valid[^1] + 10) % 10).Distinct().Order();

            await Assert.That(separators.SequenceEqual(Enumerable.Range(0, SeparatorIndexes))).IsTrue();
            await Assert.That(checkDigitShifts.SequenceEqual(Enumerable.Range(1, 9))).IsTrue();
        }

        [Test]
        public async Task CharacterDefects_ReachEveryPositionAndDigit()
        {
            var length = OfficialDrafts.LegalPersons[0].Render().Length;
            var replaced = SamplesOf(IdDefect.NonAsciiDigit)
                .Select(s => s.Id.Text.Index().First(c => !char.IsAscii(c.Item)).Index).Distinct().Order();
            var tooLong = SamplesOf(IdDefect.TooLong);
            var insertedDigits = tooLong.Select(s => InsertedDigit(s.Id.Text, s.Valid)).Distinct().Order();
            var insertedAt = tooLong.Where(s => HasOneInsertionPosition(s.Id.Text, s.Valid))
                .Select(s => CommonPrefix(s.Id.Text, s.Valid)).Distinct().Order();
            var tooShort = SamplesOf(IdDefect.TooShort);
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
                [.. SamplesOf(IdDefect.TooShort), .. SamplesOf(IdDefect.NonAsciiDigit)];

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
            await Assert.That(() => DefectCatalog.LegalPerson.Corrupt((IdDefect)(-1), draft, new Random(Seed).Next))
                .Throws<ArgumentOutOfRangeException>();
        }
    }
}
