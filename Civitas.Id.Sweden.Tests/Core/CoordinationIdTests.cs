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
    // Skatteverket-published from Testsamordningsnummer_2019_corrected.csv row 1:
    // Male, born 1914-01-08 (encoded day 68), age 111 vs 2025-12-01.
    private const string ValidPin12 = "191401682396";
    private const string ValidPin10 = "1401682396";
    private const string ValidPinWithHyphen = "140168-2396";

    // Row "first female": 191406652386 — Female, born 1914-06-05 (encoded day 65)
    private const string ValidPinFemale = "191406652386";

    public class Parsing
    {
        [Test]
        [Arguments(ValidPin12)]
        [Arguments(ValidPinWithHyphen)]
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
            // Encoded day 18 (a real day, not 78) — would be a personnummer, not coordination
            await Assert.That(CoordinationId.IsValid("191401182396")).IsFalse();
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
            await Assert.That(dayBefore).Contains("-");
            await Assert.That(dayBefore).DoesNotContain("+");
        }

        [Test]
        public async Task Format_LongFormatWithSeparator_OnCentenarianBirthday_UsesPlus()
        {
            var id = CoordinationId.Parse(Born19250601Sn);
            var birthday = id.Format(PnrFormat.LongFormatWithSeparator, new DateOnly(2025, 6, 1));
            await Assert.That(birthday).Contains("+");
        }

        [Test]
        public async Task Format_ShortFormatWithSeparator_DateOnly_HonoursToday()
        {
            var id = CoordinationId.Parse(Born19250601Sn);
            var dayBefore = id.Format(PnrFormat.ShortFormatWithSeparator, new DateOnly(2025, 5, 31));
            var birthday = id.Format(PnrFormat.ShortFormatWithSeparator, new DateOnly(2025, 6, 1));
            await Assert.That(dayBefore).Contains("-");
            await Assert.That(birthday).Contains("+");
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
            var ok = CoordinationId.TryParse(ValidPin12.AsSpan(), null, out var id);
            await Assert.That(ok).IsTrue();
            await Assert.That(id).IsNotNull();
        }

        [Test]
        public async Task ISpanParsable_Parse_AcceptsSpan()
        {
            var id = CoordinationId.Parse(ValidPin12.AsSpan(), null);
            await Assert.That(id).IsNotNull();
        }

        [Test]
        public async Task ISpanParsable_Parse_InvalidThrows()
        {
            await Assert.That(() => CoordinationId.Parse("nope".AsSpan(), null))
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
            // 191401682396 — gender digit '9' (position 10), odd → male
            var id = CoordinationId.Parse(ValidPin12);
            await Assert.That(id.IsMale).IsTrue();
            await Assert.That(id.IsFemale).IsFalse();
        }

        [Test]
        public async Task IsFemale_ForEvenGenderDigit()
        {
            // 191406652386 — gender digit '8' (position 10), even → female
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
            await Assert.That(id.ShortFormat()).IsEqualTo(ValidPin10);
        }

        [Test]
        public async Task Format_LongFormatWithStandardSeparator()
        {
            var id = CoordinationId.Parse(ValidPin12);
            await Assert.That(id.Format(PnrFormat.LongFormatWithStandardSeparator))
                .IsEqualTo("19140168-2396");
        }

        [Test]
        public async Task Format_ShortFormatWithStandardSeparator()
        {
            var id = CoordinationId.Parse(ValidPin12);
            await Assert.That(id.Format(PnrFormat.ShortFormatWithStandardSeparator))
                .IsEqualTo("140168-2396");
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
        // Born 2008-05-08 (encoded day 68), valid Luhn.
        private const string Coord20080508 = "200805680006";

        private static FakeTimeProvider StockholmFake(DateTimeOffset utcNow)
        {
            return new FakeTimeProvider(utcNow);
        }

        [Test]
        public async Task GetAge_OnExactBirthdayMidnightStockholm_Returns18()
        {
            // 2026-05-08 00:30 CEST = 2026-05-07 22:30 UTC.
            var fake = StockholmFake(new DateTimeOffset(2026, 5, 7, 22, 30, 0, TimeSpan.Zero));
            var id = CoordinationId.Parse(Coord20080508);
            await Assert.That(id.GetAge(fake)).IsEqualTo(18);
        }

        [Test]
        public async Task GetAge_NullTimeProvider_Throws()
        {
            var id = CoordinationId.Parse(Coord20080508);
            await Assert.That(() => id.GetAge(null!)).Throws<ArgumentNullException>();
        }

        [Test]
        public async Task IsAdult_OnExactBirthdayMidnightStockholm_True()
        {
            var fake = StockholmFake(new DateTimeOffset(2026, 5, 7, 22, 30, 0, TimeSpan.Zero));
            var id = CoordinationId.Parse(Coord20080508);
            await Assert.That(id.IsAdult(fake)).IsTrue();
        }

        [Test]
        public async Task IsAdult_NullTimeProvider_Throws()
        {
            var id = CoordinationId.Parse(Coord20080508);
            await Assert.That(() => id.IsAdult(null!)).Throws<ArgumentNullException>();
        }

        [Test]
        public async Task IsChild_NullTimeProvider_Throws()
        {
            var id = CoordinationId.Parse(Coord20080508);
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
            var id = CoordinationId.Parse("080568-0006", fake);
            await Assert.That(id.BirthDate).IsEqualTo(new DateOnly(2008, 5, 8));
        }

        [Test]
        public async Task Parse_NullTimeProvider_Throws()
        {
            await Assert.That(() => CoordinationId.Parse("080568-0006", null!))
                .Throws<ArgumentNullException>();
        }

        [Test]
        public async Task TryParse_TwoDigitYear_UsesProviderYearForCenturyInference()
        {
            var fake = StockholmFake(new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero));
            var ok = CoordinationId.TryParse("080568-0006", fake, out var id);
            await Assert.That(ok).IsTrue();
            await Assert.That(id!.BirthDate).IsEqualTo(new DateOnly(2008, 5, 8));
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
            await Assert.That(() => CoordinationId.TryParse("080568-0006", null!, out _))
                .Throws<ArgumentNullException>();
        }
    }

    public class TimeProviderFormatting
    {
        // Born 1925-06-01 (encoded day 61), valid Luhn (check=1). Centenarian on 2025-06-01.
        private const string Born19250601Centenarian = "192506610001";
        private const string Born20080508 = "200805680006";

        private static FakeTimeProvider StockholmFake(DateTimeOffset utcNow)
        {
            return new FakeTimeProvider(utcNow);
        }

        [Test]
        public async Task Format_LongFormatWithSeparator_BeforeCentenarianBirthday_UsesHyphen()
        {
            // 2025-05-31 12:00 CEST (UTC 10:00) — age 99, separator "-"
            var fake = StockholmFake(new DateTimeOffset(2025, 5, 31, 10, 0, 0, TimeSpan.Zero));
            var id = CoordinationId.Parse(Born19250601Centenarian);

            var formatted = id.Format(PnrFormat.LongFormatWithSeparator, fake);

            await Assert.That(formatted).Contains("-");
            await Assert.That(formatted).DoesNotContain("+");
        }

        [Test]
        public async Task Format_LongFormatWithSeparator_OnCentenarianBirthdayStockholm_UsesPlus()
        {
            // 2025-06-01 00:30 CEST = 2025-05-31 22:30 UTC.
            var fake = StockholmFake(new DateTimeOffset(2025, 5, 31, 22, 30, 0, TimeSpan.Zero));
            var id = CoordinationId.Parse(Born19250601Centenarian);

            var formatted = id.Format(PnrFormat.LongFormatWithSeparator, fake);

            await Assert.That(formatted).Contains("+");
        }

        [Test]
        public async Task Format_NullTimeProvider_Throws()
        {
            var id = CoordinationId.Parse(Born20080508);
            await Assert.That(() => id.Format(PnrFormat.LongFormatWithSeparator, null!))
                .Throws<ArgumentNullException>();
        }

        [Test]
        public async Task Format_NonSeparatorEnumValue_UsesProviderForConsistencyButResultIsTimeIndependent()
        {
            var fake = StockholmFake(new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero));
            var id = CoordinationId.Parse(Born20080508);

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
            var coord = TestIdBuilder.CoordinationIdForBirthDate(birthDate, "000");

            var id = CoordinationId.Parse(coord);
            await Assert.That(id.IsAdult()).IsTrue();
            await Assert.That(id.GetAge()).IsEqualTo(18);
        }

        [Test]
        public async Task ZeroArgFormat_LongFormatWithSeparator_DelegatesThroughSwedenClock()
        {
            var today = SwedenClock.Today();
            // Construct a person reliably aged >= 100.
            var birthDate = today.AddYears(-100).AddDays(-1);
            var coord = TestIdBuilder.CoordinationIdForBirthDate(birthDate, "000");

            var id = CoordinationId.Parse(coord);
            var formatted = id.Format(PnrFormat.LongFormatWithSeparator);

            await Assert.That(formatted).Contains("+");
        }

        [Test]
        public async Task ZeroArgGetAge_AndDateOnlyOverloadWithSwedenClockToday_AreEqual()
        {
            var today = SwedenClock.Today();
            var id = CoordinationId.Parse("200805680006");
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
            var coord = TestIdBuilder.CoordinationIdForBirthDate(new DateOnly(2008, 2, 29), "000");
            var id = CoordinationId.Parse(coord);

            await Assert.That(id.GetAge(today)).IsEqualTo(expectedAge);
            await Assert.That(id.IsAdult(today)).IsEqualTo(expectedIsAdult);
        }
    }

    public class NoDayBeforeRule
    {
        private static string Coord()
        {
            return TestIdBuilder.CoordinationIdForBirthDate(new DateOnly(2008, 5, 8), "000");
        }

        [Test]
        public async Task IsAdult_OneDayBeforeBirthday_False()
        {
            var id = CoordinationId.Parse(Coord());
            await Assert.That(id.IsAdult(new DateOnly(2026, 5, 7))).IsFalse();
        }

        [Test]
        public async Task IsAdult_OnExactBirthday_True()
        {
            var id = CoordinationId.Parse(Coord());
            await Assert.That(id.IsAdult(new DateOnly(2026, 5, 8))).IsTrue();
        }

        [Test]
        public async Task GetAge_OneDayBeforeBirthday_Returns17()
        {
            var id = CoordinationId.Parse(Coord());
            await Assert.That(id.GetAge(new DateOnly(2026, 5, 7))).IsEqualTo(17);
        }

        [Test]
        public async Task GetAge_OnExactBirthday_Returns18()
        {
            var id = CoordinationId.Parse(Coord());
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
            var coord = TestIdBuilder.CoordinationIdForBirthDate(new DateOnly(2008, 5, 10), "000");
            var id = CoordinationId.Parse(coord);

            await Assert.That(id.IsAdult(sunday)).IsTrue();
            await Assert.That(id.GetAge(sunday)).IsEqualTo(18);
        }
    }

    /// <summary>
    ///     Fixture-driven fan-out mirroring the Java sibling's
    ///     <c>CoordinationIdTest.shouldBeAValidSwedishPersonalIdWithCorrectAgeAndGender</c>.
    ///     Uses a per-row reference date computed from <c>BirthDate + expectedAge</c>
    ///     (rather than a single fixture-wide pinned date) — mirrors Java's
    ///     <c>clockForExpectedAge</c> helper.
    /// </summary>
    public class FixtureDriven
    {
        public static IEnumerable<(string Pin, bool Female, bool Male, int Age, bool Adult, bool Child, bool Valid)>
            Samordningsnummer()
        {
            var rows = CsvLoader.Load("Testsamordningsnummer_2019_corrected.csv").Skip(1);
            foreach (var r in rows)
                yield return (
                    r[0],
                    bool.Parse(r[1]),
                    bool.Parse(r[2]),
                    int.Parse(r[3], CultureInfo.InvariantCulture),
                    bool.Parse(r[4]),
                    bool.Parse(r[5]),
                    bool.Parse(r[6]));
        }

        [Test]
        [MethodDataSource(nameof(Samordningsnummer))]
        public async Task ValidSamordningsnummer_HaveCorrectAgeAndGender_PerRowClock(
            string pin, bool female, bool male, int age, bool adult, bool child, bool valid)
        {
            await Assert.That(CoordinationId.IsValid(pin)).IsEqualTo(valid);
            if (!valid) return;

            var id = CoordinationId.Parse(pin);

            // Per-row reference date: birth + (age + 1) years - 1 day. Matches Java helper.
            var refDate = id.BirthDate.AddYears(age + 1).AddDays(-1);

            await Assert.That(id.IsFemale).IsEqualTo(female);
            await Assert.That(id.IsMale).IsEqualTo(male);
            await Assert.That(id.GetAge(refDate)).IsEqualTo(age);
            await Assert.That(id.IsAdult(refDate)).IsEqualTo(adult);
            await Assert.That(id.IsChild(refDate)).IsEqualTo(child);
        }
    }
}
