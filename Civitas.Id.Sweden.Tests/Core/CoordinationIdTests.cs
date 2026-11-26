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
///     Tests for <see cref="CoordinationId" />, grouped by concern via nested classes
///     (Phil Haack pattern; TUnit-supported).
/// </summary>
public class CoordinationIdTests
{
    // Male, born 1914-01-08 (encoded day 68).
    private static readonly string ValidPin12 = OfficialNumbers.CentenarianSamordningsnummer.Long;
    private static readonly string ValidPin10 = OfficialNumbers.CentenarianSamordningsnummer.Short;

    // Female, born 1914-06-05 (encoded day 65)
    private static readonly string ValidPinFemale = OfficialNumbers.CentenarianFemaleSamordningsnummer.Long;

    public class Parsing
    {
        public static IEnumerable<string> IsValidAcceptsKnownValidInputs()
        {
            yield return ValidPin12;
            yield return OfficialNumbers.ModernSamordningsnummer.ShortWithHyphen;
        }

        [Test]
        [MethodDataSource(nameof(IsValidAcceptsKnownValidInputs))]
        public async Task IsValid_AcceptsKnownValid(string input)
        {
            await Assert.That(CoordinationId.IsValid(input)).IsTrue();
        }

        [Test]
        [Arguments(null)]
        [Arguments("")]
        [Arguments("abc")]
        [Arguments("196060019802")] // month=60 — orgnummer territory
        public async Task IsValid_RejectsInvalid(string? input)
        {
            await Assert.That(CoordinationId.IsValid(input)).IsFalse();
        }

        [Test]
        public async Task Parse_Valid_ReturnsInstance()
        {
            var id = CoordinationId.Parse(ValidPin12);
            await Assert.That(id).IsNotNull();
        }

        [Test]
        public async Task Parse_Invalid_ThrowsInvalidIdNumberException()
        {
            await Assert.That(() => CoordinationId.Parse("nonsense"))
                .Throws<InvalidIdNumberException>();
        }

        [Test]
        public async Task Parse_Null_ThrowsArgumentNullException()
        {
            await Assert.That(() => CoordinationId.Parse(null!))
                .Throws<ArgumentNullException>();
        }

        [Test]
        public async Task RejectsPersonalIdDay()
        {
            // A real day, not one + 60 — a personnummer, not coordination
            await Assert.That(CoordinationId.IsValid(OfficialNumbers.Centenarian.Long)).IsFalse();
        }

        [Test]
        public async Task RejectsOrganisationMonth()
        {
            // Month 60 — orgnummer territory
            await Assert.That(CoordinationId.IsValid("196060682396")).IsFalse();
        }

        [Test]
        public async Task TryParse_Valid_ReturnsTrue()
        {
            var ok = CoordinationId.TryParse(ValidPin12, out var id);
            await Assert.That(ok).IsTrue();
            await Assert.That(id).IsNotNull();
        }

        [Test]
        public async Task TryParse_Invalid_ReturnsFalse()
        {
            var ok = CoordinationId.TryParse("garbage", out var id);
            await Assert.That(ok).IsFalse();
            await Assert.That(id).IsNull();
        }
    }

    public class ParsingWithExplicitToday
    {
        // Deterministic-by-seed valid samordningsnummer for someone born 1925-06-01,
        // used to exercise the centenarian "+" boundary on a known birthday.
        private static readonly string Born19250601Sn =
            new CoordinationIdFaker(seed: 19250601).GenerateMale(new DateOnly(1925, 6, 1)).LongFormat();

        [Test]
        public async Task Parse_StringDateOnly_TwelveDigit_ParsesRegardlessOfToday()
        {
            // 12-digit input has explicit century; today is irrelevant for century inference.
            var idFar = CoordinationId.Parse(ValidPin12, new DateOnly(2099, 1, 1));
            var idNear = CoordinationId.Parse(ValidPin12, new DateOnly(1900, 1, 1));
            await Assert.That(idFar).IsEqualTo(idNear);
            await Assert.That(idFar.BirthDate.Year).IsEqualTo(1914);
        }

        [Test]
        public async Task TryParse_StringDateOnly_NullInput_ReturnsFalseAndNullResult()
        {
            var ok = CoordinationId.TryParse(null, new DateOnly(2026, 5, 10), out var result);
            await Assert.That(ok).IsFalse();
            await Assert.That(result).IsNull();
        }

        [Test]
        public async Task TryParse_StringDateOnly_ValidInput_ReturnsTrueAndNonNullResult()
        {
            var ok = CoordinationId.TryParse(ValidPin12, new DateOnly(2026, 5, 10), out var result);
            await Assert.That(ok).IsTrue();
            await Assert.That(result).IsNotNull();
        }

        [Test]
        public async Task Parse_SpanDateOnly_EquivalentToStringOverload()
        {
            var today = new DateOnly(2026, 5, 10);
            var fromString = CoordinationId.Parse(ValidPin12, today);
            var fromSpan = CoordinationId.Parse(ValidPin12.AsSpan(), today);
            await Assert.That(fromSpan).IsEqualTo(fromString);
        }

        [Test]
        public async Task TryParse_SpanDateOnly_EquivalentToStringOverload()
        {
            var today = new DateOnly(2026, 5, 10);

            var okString = CoordinationId.TryParse(ValidPin12, today, out var fromString);
            var okSpan = CoordinationId.TryParse(ValidPin12.AsSpan(), today, out var fromSpan);

            await Assert.That(okString).IsTrue();
            await Assert.That(okSpan).IsTrue();
            await Assert.That(fromSpan).IsEqualTo(fromString);
        }

        [Test]
        public async Task Format_LongFormatWithSeparator_DayBeforeCentenarianBirthday_UsesMinus()
        {
            var id = CoordinationId.Parse(Born19250601Sn);
            var dayBefore = id.Format(PnrFormat.LongFormatWithSeparator, new DateOnly(2025, 5, 31));
            await UnprintedAssert.HasSeparator(dayBefore, '-').ConfigureAwait(false);
        }

        [Test]
        public async Task Format_LongFormatWithSeparator_OnCentenarianBirthday_UsesPlus()
        {
            var id = CoordinationId.Parse(Born19250601Sn);
            var birthday = id.Format(PnrFormat.LongFormatWithSeparator, new DateOnly(2025, 6, 1));
            await UnprintedAssert.HasSeparator(birthday, '+').ConfigureAwait(false);
        }

        [Test]
        public async Task Format_ShortFormatWithSeparator_DateOnly_HonoursToday()
        {
            var id = CoordinationId.Parse(Born19250601Sn);
            var dayBefore = id.Format(PnrFormat.ShortFormatWithSeparator, new DateOnly(2025, 5, 31));
            var birthday = id.Format(PnrFormat.ShortFormatWithSeparator, new DateOnly(2025, 6, 1));
            await UnprintedAssert.HasSeparator(dayBefore, '-').ConfigureAwait(false);
            await UnprintedAssert.HasSeparator(birthday, '+').ConfigureAwait(false);
        }

        [Test]
        public async Task DateOnlyParse_AndTimeProviderParse_AreEquivalentForSameStockholmDate()
        {
            // FakeTimeProvider pinned to UTC noon on 2026-05-10 → Stockholm civil date 2026-05-10 (CEST 14:00).
            var today = new DateOnly(2026, 5, 10);
            var fake = new FakeTimeProvider(new DateTimeOffset(2026, 5, 10, 12, 0, 0, TimeSpan.Zero));

            var fromDateOnly = CoordinationId.Parse(ValidPin12, today);
            var fromTimeProvider = CoordinationId.Parse(ValidPin12, fake);

            await Assert.That(fromDateOnly).IsEqualTo(fromTimeProvider);
        }
    }

    public class ParsableContract
    {
        [Test]
        public async Task IParsable_TryParse_DelegatesToConvenience()
        {
            var ok = TryParseGeneric<CoordinationId>(ValidPin12, out var id);
            await Assert.That(ok).IsTrue();
            await Assert.That(id).IsNotNull();
        }

        [Test]
        public async Task ISpanParsable_TryParse_AcceptsSpan()
        {
            var ok = CoordinationId.TryParse(ValidPin12.AsSpan(), CultureInfo.InvariantCulture, out var id);
            await Assert.That(ok).IsTrue();
            await Assert.That(id).IsNotNull();
        }

        [Test]
        public async Task ISpanParsable_Parse_AcceptsSpan()
        {
            var id = CoordinationId.Parse(ValidPin12.AsSpan(), CultureInfo.InvariantCulture);
            await Assert.That(id).IsNotNull();
        }

        [Test]
        public async Task ISpanParsable_Parse_InvalidThrows()
        {
            await Assert.That(() => CoordinationId.Parse("nope".AsSpan(), CultureInfo.InvariantCulture))
                .Throws<InvalidIdNumberException>();
        }

        private static bool TryParseGeneric<T>(string input, out T? value) where T : IParsable<T>
        {
            return T.TryParse(input, null, out value);
        }
    }

    public class Properties
    {
        [Test]
        public async Task BirthDate_DecodesEncodedDay()
        {
            // Encoded day 68 → real day 8
            var id = CoordinationId.Parse(ValidPin12);
            await Assert.That(id.BirthDate).IsEqualTo(new DateOnly(1914, 1, 8));
        }

        [Test]
        public async Task IsMale_ForOddGenderDigit()
        {
            // Gender digit '9' (position 10), odd → male
            var id = CoordinationId.Parse(ValidPin12);
            await Assert.That(id.IsMale).IsTrue();
            await Assert.That(id.IsFemale).IsFalse();
        }

        [Test]
        public async Task IsFemale_ForEvenGenderDigit()
        {
            // Gender digit '8' (position 10), even → female
            var id = CoordinationId.Parse(ValidPinFemale);
            await Assert.That(id.IsFemale).IsTrue();
            await Assert.That(id.IsMale).IsFalse();
        }
    }

    public class Age
    {
        [Test]
        public async Task GetAge_AgainstFixtureDate_Matches()
        {
            // Per fixture: born 1914-01-08, age 111 vs 2025-12-01
            var id = CoordinationId.Parse(ValidPin12);
            await Assert.That(id.GetAge(new DateOnly(2025, 12, 1))).IsEqualTo(111);
        }

        [Test]
        public async Task IsAdult_NoArg_TrueForCentenarian()
        {
            var id = CoordinationId.Parse(ValidPin12);
            await Assert.That(id.IsAdult()).IsTrue();
        }

        [Test]
        public async Task IsChild_FalseForCentenarian()
        {
            var id = CoordinationId.Parse(ValidPin12);
            await Assert.That(id.IsChild()).IsFalse();
        }
    }

    public class Formatting
    {
        [Test]
        public async Task LongFormat_TwelveDigitsWithEncodedDay()
        {
            var id = CoordinationId.Parse(ValidPin12);
            await Assert.That(id.LongFormat()).IsEqualTo(ValidPin12);
        }

        [Test]
        public async Task ShortFormat_TenDigitsWithEncodedDay()
        {
            var id = CoordinationId.Parse(ValidPin12);
            await UnprintedAssert.AreEqual(id.ShortFormat(), ValidPin10).ConfigureAwait(false);
        }

        [Test]
        public async Task Format_LongFormatWithStandardSeparator()
        {
            var id = CoordinationId.Parse(ValidPin12);
            await Assert.That(id.Format(PnrFormat.LongFormatWithStandardSeparator))
                .IsEqualTo(OfficialNumbers.CentenarianSamordningsnummer.LongWithHyphen);
        }

        [Test]
        public async Task Format_ShortFormatWithStandardSeparator()
        {
            var id = CoordinationId.Parse(ValidPin12);
            await UnprintedAssert.AreEqual(id.Format(PnrFormat.ShortFormatWithStandardSeparator), OfficialNumbers.CentenarianSamordningsnummer.ShortWithHyphen)
                .ConfigureAwait(false);
        }

        [Test]
        public async Task Format_LongFormatWithSeparator_PlusForCentenarian()
        {
            var id = CoordinationId.Parse(ValidPin12);
            var formatted = id.Format(PnrFormat.LongFormatWithSeparator);
            await Assert.That(formatted).EndsWith("+2396");
        }

        [Test]
        public async Task Format_InvalidEnumValue_ThrowsArgumentOutOfRange()
        {
            var id = CoordinationId.Parse(ValidPin12);
            await Assert.That(() => id.Format((PnrFormat)999))
                .Throws<ArgumentOutOfRangeException>();
        }
    }

    public class TimeProviderAge
    {
        // Born 2000-06-06 (encoded day 66).
        private static readonly string EighteenIn2018 = OfficialNumbers.ModernSamordningsnummer.Long;

        private static FakeTimeProvider StockholmFake(DateTimeOffset utcNow)
        {
            return new FakeTimeProvider(utcNow);
        }

        [Test]
        public async Task GetAge_OnExactBirthdayMidnightStockholm_Returns18()
        {
            // 2018-06-06 00:30 CEST = 2018-06-05 22:30 UTC.
            var fake = StockholmFake(new DateTimeOffset(2018, 6, 5, 22, 30, 0, TimeSpan.Zero));
            var id = CoordinationId.Parse(EighteenIn2018);
            await Assert.That(id.GetAge(fake)).IsEqualTo(18);
        }

        [Test]
        public async Task GetAge_NullTimeProvider_Throws()
        {
            var id = CoordinationId.Parse(EighteenIn2018);
            await Assert.That(() => id.GetAge(null!)).Throws<ArgumentNullException>();
        }

        [Test]
        public async Task IsAdult_OnExactBirthdayMidnightStockholm_True()
        {
            var fake = StockholmFake(new DateTimeOffset(2018, 6, 5, 22, 30, 0, TimeSpan.Zero));
            var id = CoordinationId.Parse(EighteenIn2018);
            await Assert.That(id.IsAdult(fake)).IsTrue();
        }

        [Test]
        public async Task IsAdult_NullTimeProvider_Throws()
        {
            var id = CoordinationId.Parse(EighteenIn2018);
            await Assert.That(() => id.IsAdult(null!)).Throws<ArgumentNullException>();
        }

        [Test]
        public async Task IsChild_NullTimeProvider_Throws()
        {
            var id = CoordinationId.Parse(EighteenIn2018);
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
            var id = CoordinationId.Parse(OfficialNumbers.ModernSamordningsnummer.ShortWithHyphen, fake);
            await Assert.That(id.BirthDate).IsEqualTo(OfficialNumbers.ModernSamordningsnummer.BirthDate);
        }

        [Test]
        public async Task Parse_NullTimeProvider_Throws()
        {
            await Assert.That(() => CoordinationId.Parse(OfficialNumbers.ModernSamordningsnummer.ShortWithHyphen, null!))
                .Throws<ArgumentNullException>();
        }

        [Test]
        public async Task TryParse_TwoDigitYear_UsesProviderYearForCenturyInference()
        {
            var fake = StockholmFake(new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero));
            var ok = CoordinationId.TryParse(OfficialNumbers.ModernSamordningsnummer.ShortWithHyphen, fake, out var id);
            await Assert.That(ok).IsTrue();
            await Assert.That(id!.BirthDate).IsEqualTo(OfficialNumbers.ModernSamordningsnummer.BirthDate);
        }

        [Test]
        public async Task TryParse_InvalidInput_ReturnsFalse()
        {
            var fake = StockholmFake(new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero));
            var ok = CoordinationId.TryParse("not-a-pnr", fake, out var id);
            await Assert.That(ok).IsFalse();
            await Assert.That(id).IsNull();
        }

        [Test]
        public async Task TryParse_NullTimeProvider_Throws()
        {
            await Assert.That(() => CoordinationId.TryParse(OfficialNumbers.ModernSamordningsnummer.ShortWithHyphen, null!, out _))
                .Throws<ArgumentNullException>();
        }
    }

    public class TimeProviderFormatting
    {
        // Born 1914-06-05 (encoded day 65). Centenarian on 2014-06-05.
        private static readonly string HundredIn2014 = OfficialNumbers.CentenarianFemaleSamordningsnummer.Long;
        private static readonly string EighteenIn2018 = OfficialNumbers.ModernSamordningsnummer.Long;

        private static FakeTimeProvider StockholmFake(DateTimeOffset utcNow)
        {
            return new FakeTimeProvider(utcNow);
        }

        [Test]
        public async Task Format_LongFormatWithSeparator_BeforeCentenarianBirthday_UsesHyphen()
        {
            // 2014-06-04 12:00 CEST (UTC 10:00) — age 99, separator "-"
            var fake = StockholmFake(new DateTimeOffset(2014, 6, 4, 10, 0, 0, TimeSpan.Zero));
            var id = CoordinationId.Parse(HundredIn2014);

            var formatted = id.Format(PnrFormat.LongFormatWithSeparator, fake);

            await UnprintedAssert.HasSeparator(formatted, '-').ConfigureAwait(false);
        }

        [Test]
        public async Task Format_LongFormatWithSeparator_OnCentenarianBirthdayStockholm_UsesPlus()
        {
            // 2014-06-05 00:30 CEST = 2014-06-04 22:30 UTC.
            var fake = StockholmFake(new DateTimeOffset(2014, 6, 4, 22, 30, 0, TimeSpan.Zero));
            var id = CoordinationId.Parse(HundredIn2014);

            var formatted = id.Format(PnrFormat.LongFormatWithSeparator, fake);

            await UnprintedAssert.HasSeparator(formatted, '+').ConfigureAwait(false);
        }

        [Test]
        public async Task Format_NullTimeProvider_Throws()
        {
            var id = CoordinationId.Parse(EighteenIn2018);
            await Assert.That(() => id.Format(PnrFormat.LongFormatWithSeparator, null!))
                .Throws<ArgumentNullException>();
        }

        [Test]
        public async Task Format_NonSeparatorEnumValue_UsesProviderForConsistencyButResultIsTimeIndependent()
        {
            var fake = StockholmFake(new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero));
            var id = CoordinationId.Parse(EighteenIn2018);

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
            // Skip leap-day to keep birthday construction stable; also skip when
            // the resulting day-of-month would push encoded day above 91 (impossible here).
            if (today is { Month: 2, Day: 29 }) return;

            var birthDate = today.AddYears(-18);
            var id = CoordinationId.Parse(GeneratedNumbers.CoordinationPerson(birthDate).LongFormat());
            await Assert.That(id.IsAdult()).IsTrue();
            await Assert.That(id.GetAge()).IsEqualTo(18);
        }

        [Test]
        public async Task ZeroArgFormat_LongFormatWithSeparator_DelegatesThroughSwedenClock()
        {
            var today = SwedenClock.Today();
            // Construct a person reliably aged >= 100.
            var birthDate = today.AddYears(-100).AddDays(-1);
            var id = CoordinationId.Parse(GeneratedNumbers.CoordinationPerson(birthDate).LongFormat());
            var formatted = id.Format(PnrFormat.LongFormatWithSeparator);

            await UnprintedAssert.HasSeparator(formatted, '+').ConfigureAwait(false);
        }

        [Test]
        public async Task ZeroArgGetAge_AndDateOnlyOverloadWithSwedenClockToday_AreEqual()
        {
            var today = SwedenClock.Today();
            var id = CoordinationId.Parse(OfficialNumbers.ModernSamordningsnummer.Long);
            await Assert.That(id.GetAge()).IsEqualTo(id.GetAge(today));
        }
    }

    public class LeapDayBirthday
    {
        [Test]
        [Arguments("2038-02-27", 17, false, "day before legal birthday in non-leap year")]
        [Arguments("2038-02-28", 18, true, "legal birthday in non-leap year (Lag 1930:173 §1)")]
        [Arguments("2038-03-01", 18, true, "day after legal birthday in non-leap year")]
        [Arguments("2040-02-28", 19, true, "day before real leap-day birthday in leap year")]
        [Arguments("2040-02-29", 20, true, "real leap-day birthday in leap year")]
        [Arguments("2040-03-01", 20, true, "day after leap-day birthday in leap year")]
        public async Task LeapDay_AgeAndIsAdult_FollowLag1930_173Para1(
            string todayIso, int expectedAge, bool expectedIsAdult, string scenario)
        {
            _ = scenario;
            var today = DateOnly.Parse(todayIso, CultureInfo.InvariantCulture);
            var id = CoordinationId.Parse(OfficialNumbers.Samordningsnummer(new DateOnly(2020, 2, 29)).Long);

            await Assert.That(id.GetAge(today)).IsEqualTo(expectedAge);
            await Assert.That(id.IsAdult(today)).IsEqualTo(expectedIsAdult);
        }
    }

    public class NoDayBeforeRule
    {
        private static string Coord()
        {
            return OfficialNumbers.ModernSamordningsnummer.Long;
        }

        [Test]
        public async Task IsAdult_OneDayBeforeBirthday_False()
        {
            var id = CoordinationId.Parse(Coord());
            await Assert.That(id.IsAdult(new DateOnly(2018, 6, 5))).IsFalse();
        }

        [Test]
        public async Task IsAdult_OnExactBirthday_True()
        {
            var id = CoordinationId.Parse(Coord());
            await Assert.That(id.IsAdult(new DateOnly(2018, 6, 6))).IsTrue();
        }

        [Test]
        public async Task GetAge_OneDayBeforeBirthday_Returns17()
        {
            var id = CoordinationId.Parse(Coord());
            await Assert.That(id.GetAge(new DateOnly(2018, 6, 5))).IsEqualTo(17);
        }

        [Test]
        public async Task GetAge_OnExactBirthday_Returns18()
        {
            var id = CoordinationId.Parse(Coord());
            await Assert.That(id.GetAge(new DateOnly(2018, 6, 6))).IsEqualTo(18);
        }
    }

    public class SundayBirthdayNoExtension
    {
        [Test]
        public async Task IsAdult_OnSundayBirthday_AccruesThatSundayNotMonday()
        {
            // 2018-08-12 is a Sunday. Lag (1930:173) §2 (Sunday/holiday extension)
            // applies to deadlines for actions, NOT to age accrual.
            var sunday = new DateOnly(2018, 8, 12);
            await Assert.That(sunday.DayOfWeek).IsEqualTo(DayOfWeek.Sunday);
            var id = CoordinationId.Parse(OfficialNumbers.Samordningsnummer(new DateOnly(2000, 8, 12)).Long);

            await Assert.That(id.IsAdult(sunday)).IsTrue();
            await Assert.That(id.GetAge(sunday)).IsEqualTo(18);
        }
    }
}
