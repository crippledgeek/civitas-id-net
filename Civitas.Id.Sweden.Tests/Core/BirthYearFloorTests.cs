using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Format;
using Civitas.Id.Sweden.Tests.Helpers;
using Civitas.Id.Sweden.TypeConverters;

namespace Civitas.Id.Sweden.Tests.Core;

/// <summary>The first accepted birth year, at every way in. A number is never printed.</summary>
public class BirthYearFloorTests
{
    private const int YearsPerCentury = 100;
    private const int SeparatorForms = 3;

    private static readonly IReadOnlyList<int> EarlyReferenceYears = [1, 50, 99, 100, 199, 200, 1799];

    public class ExplicitCentury
    {
        [Test]
        public async Task FirstAcceptedYear_IsAcceptedAndKept()
        {
            await Assert.That(PersonalId.Parse(BirthYearCases.Personnummer.Accepted).BirthDate.Year)
                .IsEqualTo(BirthYearCases.FirstAcceptedYear);
            await Assert.That(CoordinationId.Parse(BirthYearCases.Samordningsnummer.Accepted).BirthDate.Year)
                .IsEqualTo(BirthYearCases.FirstAcceptedYear);
            foreach (var cases in BirthYearCases.All)
                await Assert.That(OrganisationId.Parse(cases.Accepted).ToPhysicalPersonId()?.BirthDate.Year)
                    .IsEqualTo(BirthYearCases.FirstAcceptedYear);
        }

        [Test]
        public async Task LastCalendarYear_IsAccepted()
        {
            var last = DateOnly.MaxValue.Year;

            await Assert.That(PersonalId.Parse(BirthYearCases.Personnummer.BornIn(last)).BirthDate.Year)
                .IsEqualTo(last);
        }

        /// <summary>No calendar type holds year zero, so a check that asks one about it throws.</summary>
        [Test]
        [Arguments(0)]
        [Arguments(1)]
        [Arguments(1799)]
        public async Task EarlyYear_IsAcceptedByNoEntryPoint(int year)
        {
            foreach (var cases in BirthYearCases.All)
                await Assert.That(ParserProbe.AcceptedBy(cases.BornIn(year))).IsEmpty();
        }

        [Test]
        public async Task EarlyYears_AreRejectedByEveryEntryPointForTheReasonOfAWrongCheckDigit()
        {
            foreach (var cases in BirthYearCases.All)
            {
                var expected = ThrowingParserProbe.ReasonsFor(cases.WithWrongCheckDigit);

                await Assert.That(expected.Count).IsEqualTo(ThrowingParserProbe.Count);
                await Assert.That(expected.Where(asked => asked.Reason is null)).IsEmpty();
                await Assert.That(cases.Early.IndexesWhere(text =>
                    !ThrowingParserProbe.ReasonsFor(text).SequenceEqual(expected))).IsEmpty();
                await Assert.That(cases.Early.IndexesWhere(text => ParserProbe.AcceptedBy(text).Count > 0)).IsEmpty();
            }
        }

        /// <summary>The accepted number shows that only the year stands in the way of these forms.</summary>
        [Test]
        public async Task DecoratedForms_AreAcceptedFromTheFirstAcceptedYear()
        {
            IReadOnlyList<(string EntryPoint, BirthYearCases Cases)> kinds =
            [
                ("PersonalId/plain", BirthYearCases.Personnummer),
                ("CoordinationId/plain", BirthYearCases.Samordningsnummer)
            ];
            foreach (var (entryPoint, cases) in kinds)
            {
                foreach (var text in BirthYearForms.DecoratedForms(cases.Accepted))
                {
                    await Assert.That(ParserProbe.AcceptedBy(text)).Contains(entryPoint);
                    await Assert.That(ParserProbe.AcceptedBy(text)).Contains("OrganisationId/plain");
                }

                await Assert.That(cases.Early.SelectMany(BirthYearForms.DecoratedForms)
                    .IndexesWhere(text => ParserProbe.AcceptedBy(text).Count > 0)).IsEmpty();
            }
        }

        [Test]
        public async Task LegalPerson_HasNoBirthYear()
        {
            var number = LegalPersonNumbers.Aktiebolag;

            await Assert.That(OrganisationId.IsValid(number.Digits)).IsTrue();
            await Assert.That(OrganisationId.IsValid(number.WithSixteenPrefix)).IsTrue();
        }
    }

    public class InferredCentury
    {
        [Test]
        public async Task EarlyReferenceYear_RejectsEveryShortForm()
        {
            var samples = EarlyReferenceYears.SelectMany(year => BirthYearCases.All
                    .SelectMany(cases => Enumerable.Range(0, YearsPerCentury).SelectMany(cases.ShortForms))
                    .Select(text => (Today: MidYear.DateIn(year), Clock: MidYear.ClockIn(year), Text: text)))
                .ToList();

            await Assert.That(samples.Count)
                .IsEqualTo(EarlyReferenceYears.Count * BirthYearCases.All.Count * YearsPerCentury * SeparatorForms);
            await Assert.That(samples.IndexesWhere(s =>
                PersonalId.TryParse(s.Text, s.Today, out _) || PersonalId.TryParse(s.Text.AsSpan(), s.Today, out _) ||
                PersonalId.TryParse(s.Text, s.Clock, out _))).IsEmpty();
            await Assert.That(samples.IndexesWhere(s =>
                CoordinationId.TryParse(s.Text, s.Today, out _) ||
                CoordinationId.TryParse(s.Text.AsSpan(), s.Today, out _) ||
                CoordinationId.TryParse(s.Text, s.Clock, out _))).IsEmpty();
            await Assert.That(samples.IndexesWhere(s =>
                SwedishOfficialId.TryParseAny(s.Text, s.Today, out _) ||
                SwedishOfficialId.TryParseAny(s.Text.AsSpan(), s.Today, out _) ||
                SwedishOfficialId.TryParseAny(s.Text, s.Clock, out _))).IsEmpty();
        }

        [Test]
        public async Task EarlyReferenceYear_ParseGivesTheReasonOfAWrongCheckDigit()
        {
            var wrong = BirthYearCases.Personnummer.WithWrongCheckDigit;
            foreach (var year in EarlyReferenceYears)
            {
                var ofPerson = Rejections.ReasonOf(() => PersonalId.Parse(wrong, MidYear.DateIn(year)));
                var ofAny = Rejections.ReasonOf(() => SwedishOfficialId.ParseAny(wrong, MidYear.ClockIn(year)));

                await Assert.That(ofPerson).IsNotNull();
                await Assert.That(ofAny).IsNotNull();
                foreach (var text in BirthYearCases.Personnummer.ShortForms(0))
                {
                    await Assert.That(Rejections.ReasonOf(() => PersonalId.Parse(text, MidYear.DateIn(year))))
                        .IsEqualTo(ofPerson);
                    await Assert.That(Rejections.ReasonOf(() => SwedishOfficialId.ParseAny(text, MidYear.ClockIn(year))))
                        .IsEqualTo(ofAny);
                }
            }
        }

        /// <summary>The birth years of the form without separator, with a hyphen and with a plus. Zero is rejected.</summary>
        [Test]
        [Arguments(1800, 0, new[] { 1800, 1800, 0 })]
        [Arguments(1800, 1, new[] { 0, 0, 0 })]
        [Arguments(1899, 99, new[] { 1899, 1899, 0 })]
        [Arguments(1900, 0, new[] { 1900, 1900, 1800 })]
        [Arguments(1900, 1, new[] { 1801, 1801, 0 })]
        [Arguments(9999, 0, new[] { 9900, 9900, 9800 })]
        public async Task ReferenceYear_DecidesWhetherAShortFormIsAccepted(
            int referenceYear, int twoDigitYear, int[] expected)
        {
            var today = MidYear.DateIn(referenceYear);

            var personal = BirthYearCases.Personnummer.ShortForms(twoDigitYear)
                .Select(text => PersonalId.TryParse(text, today, out var id) ? id.BirthDate.Year : 0);
            var coordination = BirthYearCases.Samordningsnummer.ShortForms(twoDigitYear)
                .Select(text => CoordinationId.TryParse(text, today, out var id) ? id.BirthDate.Year : 0);

            await Assert.That(personal.SequenceEqual(expected)).IsTrue();
            await Assert.That(coordination.SequenceEqual(expected)).IsTrue();
        }
    }

    public class ExtremeDates
    {
        private static readonly PersonalId Person = PersonalId.Parse(BirthYearCases.Personnummer.BornIn(2000));

        [Test]
        public async Task Format_OnAnyDate_ChoosesTheSeparator()
        {
            DateOnly[] before = [DateOnly.MinValue, new(99, 12, 31), new(100, 1, 1)];
            foreach (var today in before)
                await UnprintedAssert.HasSeparator(Person.Format(PnrFormat.ShortFormatWithSeparator, today), '-')
                    .ConfigureAwait(false);
            await UnprintedAssert
                .HasSeparator(Person.Format(PnrFormat.ShortFormatWithSeparator, DateOnly.MaxValue), '+')
                .ConfigureAwait(false);
        }

        [Test]
        public async Task Clock_AtEitherEnd_IsRead()
        {
            var first = new FixedClock(DateTimeOffset.MinValue);
            var last = new FixedClock(DateTimeOffset.MaxValue);
            var text = BirthYearCases.Personnummer.ShortForms(0)[0];

            await Assert.That(PersonalId.TryParse(text, first, out _)).IsFalse();
            await Assert.That(PersonalId.TryParse(text, last, out _)).IsTrue();
            await Assert.That(Person.IsAdult(first)).IsFalse();
            await Assert.That(Person.IsAdult(last)).IsTrue();
        }

        private sealed class FixedClock(DateTimeOffset now) : TimeProvider
        {
            public override DateTimeOffset GetUtcNow()
            {
                return now;
            }
        }
    }

    public class ConvertFrom
    {
        [Test]
        public async Task EarlyYears_AreRejectedAsAWrongCheckDigitIs()
        {
            await BirthYearFloorAssert
                .HoldsAsync(new PersonalIdTypeConverter().ConvertFromInvariantString, [BirthYearCases.Personnummer])
                .ConfigureAwait(false);
            await BirthYearFloorAssert
                .HoldsAsync(new CoordinationIdTypeConverter().ConvertFromInvariantString, [BirthYearCases.Samordningsnummer])
                .ConfigureAwait(false);
            await BirthYearFloorAssert
                .HoldsAsync(new OrganisationIdTypeConverter().ConvertFromInvariantString, BirthYearCases.All)
                .ConfigureAwait(false);
            await BirthYearFloorAssert
                .HoldsAsync(new SwedishOfficialIdTypeConverter().ConvertFromInvariantString, BirthYearCases.All)
                .ConfigureAwait(false);
        }
    }
}
