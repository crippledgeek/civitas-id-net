using System.Globalization;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Errors;
using Civitas.Id.Sweden.Fakers;
using Civitas.Id.Sweden.Format;
using Civitas.Id.Sweden.Internal;
using Civitas.Id.Sweden.Tests.Helpers;
using Microsoft.Extensions.Time.Testing;

namespace Civitas.Id.Sweden.Tests.Core;

/// <summary>
///     Tests for <see cref="PersonalId" />, grouped by concern via nested classes
///     (Phil Haack pattern; TUnit-supported).
/// </summary>
public class PersonalIdTests
{
    private static readonly string ValidPin12 = OfficialNumbers.Centenarian.Long;
    private static readonly string ValidPin10 = OfficialNumbers.Centenarian.Short;

    // '+' separator means 100+ years old, so YY=90 resolves to 1890 (same as ValidPin12).
    private static readonly string ValidPin10WithPlus = OfficialNumbers.Centenarian.ShortWithPlus;
    private static readonly string ValidPinMale = OfficialNumbers.CentenarianMale.Long;

    public class Parsing
    {
        public static IEnumerable<string> IsValidAcceptsKnownValidInputs()
        {
            yield return ValidPin12;
            yield return OfficialNumbers.Modern.Short;
            yield return OfficialNumbers.Modern.ShortWithHyphen;
        }

        [Test]
        [MethodDataSource(nameof(IsValidAcceptsKnownValidInputs))]
        public async Task IsValid_AcceptsKnownValid(string input)
        {
            await Assert.That(PersonalId.IsValid(input)).IsTrue();
        }

        [Test]
        [Arguments(null)]
        [Arguments("")]
        [Arguments("   ")]
        [Arguments("abc")]
        [Arguments("189013019802")] // invalid month 13
        [Arguments("189001329802")] // invalid day 32
        public async Task IsValid_RejectsInvalid(string? input)
        {
            await Assert.That(PersonalId.IsValid(input)).IsFalse();
        }

        [Test]
        public async Task IsValid_RejectsWrongChecksum()
        {
            await Assert.That(PersonalId.IsValid(OfficialNumbers.Centenarian.WithWrongCheckDigit)).IsFalse();
        }

        [Test]
        public async Task TryParse_Valid_ReturnsTrue()
        {
            var ok = PersonalId.TryParse(ValidPin12, out var id);
            await Assert.That(ok).IsTrue();
            await Assert.That(id).IsNotNull();
        }

        [Test]
        public async Task TryParse_Invalid_ReturnsFalse()
        {
            var ok = PersonalId.TryParse("nonsense", out var id);
            await Assert.That(ok).IsFalse();
            await Assert.That(id).IsNull();
        }

        [Test]
        public async Task Parse_Valid_ReturnsInstance()
        {
            var id = PersonalId.Parse(ValidPin12);
            await Assert.That(id).IsNotNull();
        }

        [Test]
        public async Task Parse_Invalid_ThrowsInvalidIdNumberException()
        {
            await Assert.That(() => PersonalId.Parse("nonsense"))
                .Throws<InvalidIdNumberException>();
        }

        [Test]
        public async Task Parse_Null_ThrowsArgumentNullException()
        {
            await Assert.That(() => PersonalId.Parse(null!))
                .Throws<ArgumentNullException>();
        }

        [Test]
        public async Task TwoEqualPins_AreEqual()
        {
            // ValidPin12 encodes century explicitly (1890); ValidPin10WithPlus
            // resolves to the same 1890 century via the >100-year-old '+' rule.
            var a = PersonalId.Parse(ValidPin12);
            var b = PersonalId.Parse(ValidPin10WithPlus);
            await UnprintedAssert.AreEqual(a, b).ConfigureAwait(false);
            await Assert.That(a.GetHashCode()).IsEqualTo(b.GetHashCode());
        }

        [Test]
        public async Task RejectsCoordinationDay()
        {
            // Valid but for its day, which is the real day + 60 (samordningsnummer territory)
            await Assert.That(PersonalId.IsValid(OfficialNumbers.CentenarianSamordningsnummer.Long)).IsFalse();
        }

        [Test]
        public async Task RejectsOrganisationMonth()
        {
            // Month 60 indicates organisationsnummer
            await Assert.That(PersonalId.IsValid("196060189876")).IsFalse();
        }

        [Test]
        public async Task TwelveDigitForm_PreservesExplicitCentury()
        {
            var id = PersonalId.Parse(ValidPin12);
            await Assert.That(id.LongFormat()).IsEqualTo(ValidPin12);
        }

        [Test]
        public async Task PlusSeparator_ResolvesToPreviousCentury()
        {
            // '+' separator means "100+ years old" — century inference shifts back.
            // Year 90 with the current year 2026 would otherwise infer 1990; '+' shifts to 1890.
            var id = PersonalId.Parse(ValidPin10WithPlus);
            await UnprintedAssert.AreEqual(id.LongFormat(), OfficialNumbers.Centenarian.Long).ConfigureAwait(false);
        }
    }

    public class ParsingWithExplicitToday
    {
        // Deterministic-by-seed valid personnummer for someone born 1925-06-01,
        // used to exercise the centenarian "+" boundary on a known birthday.
        private static readonly string Born19250601Pin =
            new PersonalIdFaker(seed: 19250601).GenerateMale(new DateOnly(1925, 6, 1)).LongFormat();

        [Test]
        public async Task Parse_StringDateOnly_TwelveDigit_ParsesRegardlessOfToday()
        {
            // 12-digit input has explicit century; today is irrelevant for century inference.
            var idFar = PersonalId.Parse(ValidPin12, new DateOnly(2099, 1, 1));
            var idNear = PersonalId.Parse(ValidPin12, new DateOnly(1900, 1, 1));
            await Assert.That(idFar).IsEqualTo(idNear);
            await Assert.That(idFar.BirthDate.Year).IsEqualTo(1890);
        }

        [Test]
        public async Task TryParse_StringDateOnly_NullInput_ReturnsFalseAndNullResult()
        {
            var ok = PersonalId.TryParse(null, new DateOnly(2026, 5, 10), out var result);
            await Assert.That(ok).IsFalse();
            await Assert.That(result).IsNull();
        }

        [Test]
        public async Task TryParse_StringDateOnly_ValidInput_ReturnsTrueAndNonNullResult()
        {
            var ok = PersonalId.TryParse(ValidPin12, new DateOnly(2026, 5, 10), out var result);
            await Assert.That(ok).IsTrue();
            await Assert.That(result).IsNotNull();
        }

        [Test]
        public async Task Parse_SpanDateOnly_EquivalentToStringOverload()
        {
            var today = new DateOnly(2026, 5, 10);
            var fromString = PersonalId.Parse(ValidPin12, today);
            var fromSpan = PersonalId.Parse(ValidPin12.AsSpan(), today);
            await Assert.That(fromSpan).IsEqualTo(fromString);
        }

        [Test]
        public async Task TryParse_SpanDateOnly_EquivalentToStringOverload()
        {
            var today = new DateOnly(2026, 5, 10);

            var okString = PersonalId.TryParse(ValidPin12, today, out var fromString);
            var okSpan = PersonalId.TryParse(ValidPin12.AsSpan(), today, out var fromSpan);

            await Assert.That(okString).IsTrue();
            await Assert.That(okSpan).IsTrue();
            await Assert.That(fromSpan).IsEqualTo(fromString);
        }

        [Test]
        public async Task Format_LongFormatWithSeparator_DayBeforeCentenarianBirthday_UsesMinus()
        {
            var id = PersonalId.Parse(Born19250601Pin);
            var dayBefore = id.Format(PnrFormat.LongFormatWithSeparator, new DateOnly(2025, 5, 31));
            await UnprintedAssert.HasSeparator(dayBefore, '-').ConfigureAwait(false);
        }

        [Test]
        public async Task Format_LongFormatWithSeparator_OnCentenarianBirthday_UsesPlus()
        {
            var id = PersonalId.Parse(Born19250601Pin);
            var birthday = id.Format(PnrFormat.LongFormatWithSeparator, new DateOnly(2025, 6, 1));
            await UnprintedAssert.HasSeparator(birthday, '+').ConfigureAwait(false);
        }

        [Test]
        public async Task DateOnlyParse_AndTimeProviderParse_AreEquivalentForSameStockholmDate()
        {
            // FakeTimeProvider pinned to UTC noon on 2026-05-10 → Stockholm civil date 2026-05-10 (CEST 14:00).
            var today = new DateOnly(2026, 5, 10);
            var fake = new FakeTimeProvider(new DateTimeOffset(2026, 5, 10, 12, 0, 0, TimeSpan.Zero));

            var fromDateOnly = PersonalId.Parse(ValidPin12, today);
            var fromTimeProvider = PersonalId.Parse(ValidPin12, fake);

            await Assert.That(fromDateOnly).IsEqualTo(fromTimeProvider);
        }
    }

    public class ParsableContract
    {
        [Test]
        public async Task IParsable_Parse_DelegatesToConvenience()
        {
            var result = ParseGeneric<PersonalId>(ValidPin12);
            await Assert.That(result).IsNotNull();
        }

        [Test]
        public async Task IParsable_TryParse_DelegatesToConvenience()
        {
            var ok = TryParseGeneric<PersonalId>(ValidPin12, out var id);
            await Assert.That(ok).IsTrue();
            await Assert.That(id).IsNotNull();
        }

        [Test]
        public async Task ISpanParsable_TryParse_AcceptsSpan()
        {
            var ok = PersonalId.TryParse(ValidPin12.AsSpan(), CultureInfo.InvariantCulture, out var id);
            await Assert.That(ok).IsTrue();
            await Assert.That(id).IsNotNull();
        }

        [Test]
        public async Task ISpanParsable_Parse_ThrowsOnInvalidSpan()
        {
            await Assert.That(() => PersonalId.Parse("garbage".AsSpan(), CultureInfo.InvariantCulture))
                .Throws<InvalidIdNumberException>();
        }

        [Test]
        public async Task ISpanParsable_TryParse_ReturnsFalseOnInvalid()
        {
            var ok = PersonalId.TryParse("nonsense".AsSpan(), CultureInfo.InvariantCulture, out var id);
            await Assert.That(ok).IsFalse();
            await Assert.That(id).IsNull();
        }

        [Test]
        public async Task ISpanParsable_Parse_AcceptsValidSpan()
        {
            var id = PersonalId.Parse(ValidPin12.AsSpan(), CultureInfo.InvariantCulture);
            await Assert.That(id).IsNotNull();
        }

        private static T ParseGeneric<T>(string input) where T : IParsable<T>
        {
            return T.Parse(input, null);
        }

        private static bool TryParseGeneric<T>(string input, out T? value) where T : IParsable<T>
        {
            return T.TryParse(input, null, out value);
        }
    }

    public class Properties
    {
        [Test]
        public async Task BirthDate_Computed()
        {
            var id = PersonalId.Parse(ValidPin12);
            await Assert.That(id.BirthDate).IsEqualTo(new DateOnly(1890, 1, 1));
        }

        [Test]
        public async Task IsFemale_ForEvenGenderDigit()
        {
            // Gender digit '0' (position 10), even → female
            var id = PersonalId.Parse(ValidPin12);
            await Assert.That(id.IsFemale).IsTrue();
            await Assert.That(id.IsMale).IsFalse();
        }

        [Test]
        public async Task IsMale_ForOddGenderDigit()
        {
            // Gender digit '1' (position 10), odd → male
            var id = PersonalId.Parse(ValidPinMale);
            await Assert.That(id.IsMale).IsTrue();
            await Assert.That(id.IsFemale).IsFalse();
        }
    }

    public class Age
    {
        [Test]
        public async Task GetAge_BeforeBirthday_OneLess()
        {
            var id = PersonalId.Parse(ValidPin12);
            var dayBefore = new DateOnly(1995, 12, 31);
            await Assert.That(id.GetAge(dayBefore)).IsEqualTo(105);
        }

        [Test]
        public async Task GetAge_OnBirthday_FullYears()
        {
            var id = PersonalId.Parse(ValidPin12);
            var birthday = new DateOnly(1996, 1, 1);
            await Assert.That(id.GetAge(birthday)).IsEqualTo(106);
        }

        [Test]
        public async Task GetAge_AfterBirthday_FullYears()
        {
            var id = PersonalId.Parse(ValidPin12);
            var dayAfter = new DateOnly(1996, 1, 2);
            await Assert.That(id.GetAge(dayAfter)).IsEqualTo(106);
        }

        [Test]
        public async Task IsAdult_OnExact18thBirthday_True()
        {
            var id = PersonalId.Parse(ValidPin12);
            var eighteenthBirthday = new DateOnly(1908, 1, 1);
            await Assert.That(id.IsAdult(eighteenthBirthday)).IsTrue();
            await Assert.That(id.IsChild(eighteenthBirthday)).IsFalse();
        }

        [Test]
        public async Task IsChild_DayBefore18thBirthday_True()
        {
            var id = PersonalId.Parse(ValidPin12);
            var dayBefore18 = new DateOnly(1907, 12, 31);
            await Assert.That(id.IsChild(dayBefore18)).IsTrue();
            await Assert.That(id.IsAdult(dayBefore18)).IsFalse();
        }

        [Test]
        public async Task GetAge_NoArg_ReturnsNonNegative()
        {
            var id = PersonalId.Parse(ValidPin12);
            await Assert.That(id.GetAge()).IsGreaterThanOrEqualTo(0);
        }

        [Test]
        public async Task IsAdult_NoArg_TrueForCentenarian()
        {
            // Born 1890 → adult today regardless of date
            var id = PersonalId.Parse(ValidPin12);
            await Assert.That(id.IsAdult()).IsTrue();
        }
    }

    public class Formatting
    {
        [Test]
        public async Task LongFormat_TwelveDigits()
        {
            var id = PersonalId.Parse(ValidPin12);
            await Assert.That(id.LongFormat()).IsEqualTo(OfficialNumbers.Centenarian.Long);
        }

        [Test]
        public async Task ShortFormat_TenDigits()
        {
            var id = PersonalId.Parse(ValidPin12);
            await UnprintedAssert.AreEqual(id.ShortFormat(), OfficialNumbers.Centenarian.Short).ConfigureAwait(false);
        }

        [Test]
        public async Task Format_LongFormat()
        {
            var id = PersonalId.Parse(ValidPin12);
            await Assert.That(id.Format(PnrFormat.LongFormat)).IsEqualTo(OfficialNumbers.Centenarian.Long);
        }

        [Test]
        public async Task Format_ShortFormat()
        {
            var id = PersonalId.Parse(ValidPin12);
            await UnprintedAssert.AreEqual(id.Format(PnrFormat.ShortFormat), OfficialNumbers.Centenarian.Short)
                .ConfigureAwait(false);
        }

        [Test]
        public async Task Format_LongFormatWithStandardSeparator_AlwaysHyphen()
        {
            var id = PersonalId.Parse(ValidPin12);
            await Assert.That(id.Format(PnrFormat.LongFormatWithStandardSeparator))
                .IsEqualTo(OfficialNumbers.Centenarian.LongWithHyphen);
        }

        [Test]
        public async Task Format_ShortFormatWithStandardSeparator_AlwaysHyphen()
        {
            var id = PersonalId.Parse(ValidPin12);
            await UnprintedAssert.AreEqual(id.Format(PnrFormat.ShortFormatWithStandardSeparator), OfficialNumbers.Centenarian.ShortWithHyphen)
                .ConfigureAwait(false);
        }

        [Test]
        public async Task Format_LongFormatWithSeparator_PlusForCentenarian()
        {
            var id = PersonalId.Parse(ValidPin12);
            var formatted = id.Format(PnrFormat.LongFormatWithSeparator);
            await Assert.That(formatted).IsEqualTo(OfficialNumbers.Centenarian.LongWithPlus);
        }

        [Test]
        public async Task Format_ShortFormatWithSeparator_PlusForCentenarian()
        {
            var id = PersonalId.Parse(ValidPin12);
            var formatted = id.Format(PnrFormat.ShortFormatWithSeparator);
            await UnprintedAssert.AreEqual(formatted, OfficialNumbers.Centenarian.ShortWithPlus).ConfigureAwait(false);
        }

        [Test]
        public async Task Format_LongFormatWithSeparator_HyphenForUnderHundred()
        {
            // The short form parsed today → century inferred as 1990 → age ~35 → '-'
            var id = PersonalId.Parse(ValidPin10);
            var formatted = id.Format(PnrFormat.LongFormatWithSeparator);
            await Assert.That(id.BirthDate).IsEqualTo(new DateOnly(1990, 1, 1));
            await UnprintedAssert.AreEqual(formatted, $"19900101-{OfficialNumbers.Centenarian.Serial}")
                .ConfigureAwait(false);
        }

        [Test]
        public async Task Format_InvalidEnumValue_ThrowsArgumentOutOfRange()
        {
            var id = PersonalId.Parse(ValidPin12);
            await Assert.That(() => id.Format((PnrFormat)999))
                .Throws<ArgumentOutOfRangeException>();
        }
    }

    public class TimeProviderAge
    {
        private static readonly string EighteenIn2026 = OfficialNumbers.EighteenIn2026.Long;

        private static FakeTimeProvider StockholmFake(DateTimeOffset utcNow)
        {
            return new FakeTimeProvider(utcNow);
        }

        [Test]
        public async Task GetAge_OnExactBirthdayMidnightStockholm_Returns18()
        {
            // 2026-05-08 00:30 CEST = 2026-05-07 22:30 UTC
            var fake = StockholmFake(new DateTimeOffset(2026, 5, 7, 22, 30, 0, TimeSpan.Zero));
            var id = PersonalId.Parse(EighteenIn2026);

            await Assert.That(id.GetAge(fake)).IsEqualTo(18);
        }

        [Test]
        public async Task GetAge_OneDayBeforeBirthdayStockholm_Returns17()
        {
            // 2026-05-07 12:00 CEST = 2026-05-07 10:00 UTC
            var fake = StockholmFake(new DateTimeOffset(2026, 5, 7, 10, 0, 0, TimeSpan.Zero));
            var id = PersonalId.Parse(EighteenIn2026);

            await Assert.That(id.GetAge(fake)).IsEqualTo(17);
        }

        [Test]
        public async Task GetAge_NullTimeProvider_Throws()
        {
            var id = PersonalId.Parse(EighteenIn2026);
            await Assert.That(() => id.GetAge(null!))
                .Throws<ArgumentNullException>();
        }

        [Test]
        public async Task GetAge_DelegatesToDateOnlyOverload()
        {
            // Verify the TimeProvider path produces the same answer as
            // calling GetAge(DateOnly today) directly with the equivalent date.
            var fake = StockholmFake(new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero));
            var id = PersonalId.Parse(EighteenIn2026);

            var viaProvider = id.GetAge(fake);
            var viaDateOnly = id.GetAge(new DateOnly(2026, 6, 15));

            await Assert.That(viaProvider).IsEqualTo(viaDateOnly);
        }

        [Test]
        public async Task IsAdult_OnExactBirthdayMidnightStockholm_True()
        {
            var fake = StockholmFake(new DateTimeOffset(2026, 5, 7, 22, 30, 0, TimeSpan.Zero));
            var id = PersonalId.Parse(EighteenIn2026);
            await Assert.That(id.IsAdult(fake)).IsTrue();
        }

        [Test]
        public async Task IsAdult_OneDayBeforeBirthdayStockholm_False()
        {
            var fake = StockholmFake(new DateTimeOffset(2026, 5, 7, 10, 0, 0, TimeSpan.Zero));
            var id = PersonalId.Parse(EighteenIn2026);
            await Assert.That(id.IsAdult(fake)).IsFalse();
        }

        [Test]
        public async Task IsChild_OnExactBirthdayMidnightStockholm_False()
        {
            var fake = StockholmFake(new DateTimeOffset(2026, 5, 7, 22, 30, 0, TimeSpan.Zero));
            var id = PersonalId.Parse(EighteenIn2026);
            await Assert.That(id.IsChild(fake)).IsFalse();
        }

        [Test]
        public async Task IsAdult_NullTimeProvider_Throws()
        {
            var id = PersonalId.Parse(EighteenIn2026);
            await Assert.That(() => id.IsAdult(null!)).Throws<ArgumentNullException>();
        }

        [Test]
        public async Task IsChild_NullTimeProvider_Throws()
        {
            var id = PersonalId.Parse(EighteenIn2026);
            await Assert.That(() => id.IsChild(null!)).Throws<ArgumentNullException>();
        }
    }

    public class TimeProviderParsing
    {
        private static FakeTimeProvider StockholmFake(DateTimeOffset utcNow)
        {
            return new FakeTimeProvider(utcNow);
        }

        [Test]
        public async Task Parse_TwoDigitYear_UsesProviderYearForCenturyInference()
        {
            var fake = StockholmFake(new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero));
            var id = PersonalId.Parse(OfficialNumbers.EighteenIn2026.ShortWithHyphen, fake);

            await Assert.That(id.BirthDate).IsEqualTo(new DateOnly(2008, 5, 8));
        }

        [Test]
        public async Task Parse_NullTimeProvider_Throws()
        {
            await Assert.That(() => PersonalId.Parse(OfficialNumbers.EighteenIn2026.ShortWithHyphen, null!))
                .Throws<ArgumentNullException>();
        }

        [Test]
        public async Task TryParse_TwoDigitYear_UsesProviderYearForCenturyInference()
        {
            var fake = StockholmFake(new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero));
            var ok = PersonalId.TryParse(OfficialNumbers.EighteenIn2026.ShortWithHyphen, fake, out var id);

            await Assert.That(ok).IsTrue();
            await Assert.That(id!.BirthDate).IsEqualTo(new DateOnly(2008, 5, 8));
        }

        [Test]
        public async Task TryParse_InvalidInput_ReturnsFalse()
        {
            var fake = StockholmFake(new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero));
            var ok = PersonalId.TryParse("not-a-pnr", fake, out var id);

            await Assert.That(ok).IsFalse();
            await Assert.That(id).IsNull();
        }

        [Test]
        public async Task TryParse_NullTimeProvider_Throws()
        {
            await Assert.That(() => PersonalId.TryParse(OfficialNumbers.EighteenIn2026.ShortWithHyphen, null!, out _))
                .Throws<ArgumentNullException>();
        }
    }

    public class TimeProviderFormatting
    {
        private static readonly string HundredIn1995 = OfficialNumbers.HundredIn1995.Long;
        private static readonly string EighteenIn2026 = OfficialNumbers.EighteenIn2026.Long;

        private static FakeTimeProvider StockholmFake(DateTimeOffset utcNow)
        {
            return new FakeTimeProvider(utcNow);
        }

        [Test]
        public async Task Format_LongFormatWithSeparator_BeforeCentenarianBirthday_UsesHyphen()
        {
            // 1995-05-31 12:00 CEST (UTC 10:00) — age 99, separator "-"
            var fake = StockholmFake(new DateTimeOffset(1995, 5, 31, 10, 0, 0, TimeSpan.Zero));
            var id = PersonalId.Parse(HundredIn1995);

            var formatted = id.Format(PnrFormat.LongFormatWithSeparator, fake);

            await UnprintedAssert.HasSeparator(formatted, '-').ConfigureAwait(false);
        }

        [Test]
        public async Task Format_LongFormatWithSeparator_OnCentenarianBirthdayStockholm_UsesPlus()
        {
            // 1995-06-01 00:30 CEST = 1995-05-31 22:30 UTC — Stockholm has crossed midnight,
            // person is age 100; UTC anchor would still say age 99 ("-").
            var fake = StockholmFake(new DateTimeOffset(1995, 5, 31, 22, 30, 0, TimeSpan.Zero));
            var id = PersonalId.Parse(HundredIn1995);

            var formatted = id.Format(PnrFormat.LongFormatWithSeparator, fake);

            await UnprintedAssert.HasSeparator(formatted, '+').ConfigureAwait(false);
        }

        [Test]
        public async Task Format_NullTimeProvider_Throws()
        {
            var id = PersonalId.Parse(EighteenIn2026);
            await Assert.That(() => id.Format(PnrFormat.LongFormatWithSeparator, null!))
                .Throws<ArgumentNullException>();
        }

        [Test]
        public async Task Format_NonSeparatorEnumValue_UsesProviderForConsistencyButResultIsTimeIndependent()
        {
            // ShortFormat doesn't embed a "+/-" — result identical with/without provider.
            var fake = StockholmFake(new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero));
            var id = PersonalId.Parse(EighteenIn2026);

            var withProvider = id.Format(PnrFormat.ShortFormat, fake);
            var withoutProvider = id.Format(PnrFormat.ShortFormat);

            await Assert.That(withProvider).IsEqualTo(withoutProvider);
        }
    }

    public class StockholmPin
    {
        [Test]
        public async Task ZeroArgGetAge_AnchorsToSwedenClockToday()
        {
            var today = SwedenClock.Today();
            // Skip the test on leap-day to keep birthday construction stable.
            if (today is { Month: 2, Day: 29 }) return;

            var birthDate = today.AddYears(-18);
            var id = PersonalId.Parse(GeneratedNumbers.Person(birthDate).LongFormat());
            await Assert.That(id.IsAdult()).IsTrue();
            await Assert.That(id.GetAge()).IsEqualTo(18);
        }

        [Test]
        public async Task ZeroArgFormat_LongFormatWithSeparator_DelegatesThroughSwedenClock()
        {
            var today = SwedenClock.Today();
            // Construct a person reliably aged >= 100.
            var birthDate = today.AddYears(-100).AddDays(-1);
            var id = PersonalId.Parse(GeneratedNumbers.Person(birthDate).LongFormat());

            var formatted = id.Format(PnrFormat.LongFormatWithSeparator);

            await UnprintedAssert.HasSeparator(formatted, '+').ConfigureAwait(false);
        }

        [Test]
        public async Task ZeroArgGetAge_AndDateOnlyOverloadWithSwedenClockToday_AreEqual()
        {
            var today = SwedenClock.Today();
            var id = PersonalId.Parse(OfficialNumbers.EighteenIn2026.Long);

            await Assert.That(id.GetAge()).IsEqualTo(id.GetAge(today));
        }
    }

    public class LeapDayBirthday
    {
        [Test]
        [Arguments("2026-02-27", 17, false, "day before legal birthday in non-leap year")]
        [Arguments("2026-02-28", 18, true, "legal birthday in non-leap year (Lag 1930:173 §1)")]
        [Arguments("2026-03-01", 18, true, "day after legal birthday in non-leap year")]
        [Arguments("2028-02-28", 19, true, "day before real leap-day birthday in leap year")]
        [Arguments("2028-02-29", 20, true, "real leap-day birthday in leap year")]
        [Arguments("2028-03-01", 20, true, "day after leap-day birthday in leap year")]
        public async Task LeapDay_AgeAndIsAdult_FollowLag1930_173Para1(
            string todayIso, int expectedAge, bool expectedIsAdult, string scenario)
        {
            _ = scenario;
            var today = DateOnly.Parse(todayIso, CultureInfo.InvariantCulture);
            var id = PersonalId.Parse(OfficialNumbers.Personnummer(new DateOnly(2008, 2, 29)).Long);

            await Assert.That(id.GetAge(today)).IsEqualTo(expectedAge);
            await Assert.That(id.IsAdult(today)).IsEqualTo(expectedIsAdult);
        }
    }

    public class NoDayBeforeRule
    {
        private static string Pnr()
        {
            return OfficialNumbers.EighteenIn2026.Long;
        }

        [Test]
        public async Task IsAdult_OneDayBeforeBirthday_False()
        {
            var id = PersonalId.Parse(Pnr());
            await Assert.That(id.IsAdult(new DateOnly(2026, 5, 7))).IsFalse();
        }

        [Test]
        public async Task IsAdult_OnExactBirthday_True()
        {
            var id = PersonalId.Parse(Pnr());
            await Assert.That(id.IsAdult(new DateOnly(2026, 5, 8))).IsTrue();
        }

        [Test]
        public async Task GetAge_OneDayBeforeBirthday_Returns17()
        {
            var id = PersonalId.Parse(Pnr());
            await Assert.That(id.GetAge(new DateOnly(2026, 5, 7))).IsEqualTo(17);
        }

        [Test]
        public async Task GetAge_OnExactBirthday_Returns18()
        {
            var id = PersonalId.Parse(Pnr());
            await Assert.That(id.GetAge(new DateOnly(2026, 5, 8))).IsEqualTo(18);
        }
    }

    public class SundayBirthdayNoExtension
    {
        [Test]
        public async Task IsAdult_OnSundayBirthday_AccruesThatSundayNotMonday()
        {
            // 2026-05-10 is a Sunday. Lag (1930:173) §2 (Sunday/holiday extension)
            // applies to deadlines for actions, NOT to age accrual.
            var sunday = new DateOnly(2026, 5, 10);
            await Assert.That(sunday.DayOfWeek).IsEqualTo(DayOfWeek.Sunday);
            var id = PersonalId.Parse(OfficialNumbers.Personnummer(new DateOnly(2008, 5, 10)).Long);

            await Assert.That(id.IsAdult(sunday)).IsTrue();
            await Assert.That(id.GetAge(sunday)).IsEqualTo(18);
        }
    }

    /// <summary>
    ///     Fixture-driven fan-out mirroring the Java sibling's
    ///     <c>PersonalIdTest.personalNumbersShouldBeValid</c>.
    /// </summary>
    public class FixtureDriven
    {
        public static IEnumerable<string> Personnummer1890S()
        {
            return CsvLoader.Column(Fixtures.Personnummer1890S);
        }

        [Test]
        [MethodDataSource(nameof(Personnummer1890S))]
        public async Task IsValid_Personnummer1890s_ReturnsTrue(string pin)
        {
            await Assert.That(PersonalId.IsValid(pin)).IsTrue();
        }
    }
}
