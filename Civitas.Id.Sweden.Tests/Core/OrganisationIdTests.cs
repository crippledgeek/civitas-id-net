using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Errors;
using Civitas.Id.Sweden.Format;
using Civitas.Id.Sweden.Tests.Helpers;
using Microsoft.Extensions.Time.Testing;

namespace Civitas.Id.Sweden.Tests.Core;

/// <summary>
///     Tests for <see cref="OrganisationId" />, grouped by concern.
/// </summary>
public class OrganisationIdTests
{
    // From testorganisationsnummer_extended.csv:
    // 5560160680 — Aktiebolag, valid (form code "55" → EuropakooperativEgtsEric)
    private const string ValidShort = "5560160680";
    private const string ValidLong = "165560160680"; // 12-digit form
    private const string ValidWithHyphen = "556016-0680"; // hyphen separator

    public class Parsing
    {
        [Test]
        [Arguments(ValidShort)]
        [Arguments(ValidLong)]
        [Arguments(ValidWithHyphen)]
        public async Task IsValid_AcceptsKnownValid(string input)
        {
            await Assert.That(OrganisationId.IsValid(input)).IsTrue();
        }

        [Test]
        [Arguments(null)]
        [Arguments("")]
        [Arguments("abc")]
        [Arguments("5560160681")] // wrong checksum (last digit changed) — from fixture row labeled false
        [Arguments("195560160680")] // 12-digit form but century is "19", not "16"
        public async Task IsValid_RejectsInvalid(string? input)
        {
            await Assert.That(OrganisationId.IsValid(input)).IsFalse();
        }

        [Test]
        [Arguments("198112189876")] // personnummer-shape (month=12, day=18) — Enskild firma
        [Arguments("191401682396")] // samordningsnummer-shape (month=01, day=68) — Enskild firma
        public async Task IsValid_AcceptsEnskildFirma(string input)
        {
            // Re-classified from IsValid_RejectsInvalid: with Enskild firma support,
            // 12-digit person-shape orgnummer with valid Luhn now parse successfully.
            await Assert.That(OrganisationId.IsValid(input)).IsTrue();
        }

        [Test]
        public async Task Parse_TwelveDigitForm_StripsSixteenPrefix()
        {
            var id = OrganisationId.Parse(ValidLong);
            await Assert.That(id.ShortFormat()).IsEqualTo(ValidShort);
        }

        [Test]
        public async Task Parse_HyphenForm_StripsSeparator()
        {
            var id = OrganisationId.Parse(ValidWithHyphen);
            await Assert.That(id.ShortFormat()).IsEqualTo(ValidShort);
        }

        [Test]
        public async Task Parse_Invalid_ThrowsInvalidIdNumberException()
        {
            await Assert.That(() => OrganisationId.Parse("nonsense"))
                .Throws<InvalidIdNumberException>();
        }

        [Test]
        public async Task Parse_Null_ThrowsArgumentNullException()
        {
            await Assert.That(() => OrganisationId.Parse(null!))
                .Throws<ArgumentNullException>();
        }

        [Test]
        public async Task TryParse_Valid_ReturnsTrue()
        {
            var ok = OrganisationId.TryParse(ValidShort, out var id);
            await Assert.That(ok).IsTrue();
            await Assert.That(id).IsNotNull();
        }

        [Test]
        public async Task EquivalentForms_AreEqual()
        {
            // 10-digit, 12-digit-with-16-prefix, and hyphenated all canonicalise to the same thing.
            var a = OrganisationId.Parse(ValidShort);
            var b = OrganisationId.Parse(ValidLong);
            var c = OrganisationId.Parse(ValidWithHyphen);
            await Assert.That(a).IsEqualTo(b);
            await Assert.That(b).IsEqualTo(c);
            await Assert.That(a.GetHashCode()).IsEqualTo(b.GetHashCode());
        }
    }

    public class ParsableContract
    {
        [Test]
        public async Task IParsable_TryParse_DelegatesToConvenience()
        {
            var ok = TryParseGeneric<OrganisationId>(ValidShort, out var id);
            await Assert.That(ok).IsTrue();
            await Assert.That(id).IsNotNull();
        }

        [Test]
        public async Task ISpanParsable_TryParse_AcceptsSpan()
        {
            var ok = OrganisationId.TryParse(ValidShort.AsSpan(), null, out var id);
            await Assert.That(ok).IsTrue();
            await Assert.That(id).IsNotNull();
        }

        [Test]
        public async Task ISpanParsable_Parse_ThrowsOnInvalidSpan()
        {
            await Assert.That(() => OrganisationId.Parse("garbage".AsSpan(), null))
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
        public async Task Form_DerivedFromFirstTwoDigits()
        {
            // First two digits "55" → EuropakooperativEgtsEric
            var id = OrganisationId.Parse(ValidShort);
            await Assert.That(id.Form).IsEqualTo(OrganisationForm.EuropakooperativEgtsEric);
        }

        [Test]
        public async Task NumberType_LegalPerson()
        {
            var id = OrganisationId.Parse(ValidShort);
            await Assert.That(id.NumberType).IsEqualTo(OrganisationNumberType.LegalPerson);
        }

        [Test]
        public async Task Form_LegalPersonWithPersonShapedMiddleDigits_NotMisclassifiedAsPhysical()
        {
            // 5561034249 is a real Aktiebolag — positions 2-3 = "61" coincidentally falls
            // in the [61,91] samordningsnummer day-offset range, but this is a legal-person
            // orgnummer (parsed without _personCentury). Form must NOT return None.
            var id = OrganisationId.Parse("5561034249");
            await Assert.That(id.Form).IsNotEqualTo(OrganisationForm.None);
            await Assert.That(id.NumberType).IsEqualTo(OrganisationNumberType.LegalPerson);
        }
    }

    public class Formatting
    {
        [Test]
        public async Task LongFormat_LegalPerson_TenDigitsNoPrefix()
        {
            var id = OrganisationId.Parse(ValidShort);
            // Per Lag 1974:174 §4, organisationsnummer is 10 digits; "16" is input-only legacy.
            await Assert.That(id.LongFormat()).IsEqualTo("5560160680");
        }

        [Test]
        public async Task LongFormat_LegalPerson_EqualsShortFormat()
        {
            var id = OrganisationId.Parse(ValidShort);
            await Assert.That(id.LongFormat()).IsEqualTo(id.ShortFormat());
        }

        [Test]
        public async Task ShortFormat_TenDigits()
        {
            var id = OrganisationId.Parse(ValidShort);
            await Assert.That(id.ShortFormat()).IsEqualTo(ValidShort);
        }

        [Test]
        public async Task Format_ShortFormatWithStandardSeparator_HyphenAtSix()
        {
            var id = OrganisationId.Parse(ValidShort);
            await Assert.That(id.Format(PnrFormat.ShortFormatWithStandardSeparator))
                .IsEqualTo("556016-0680");
        }

        [Test]
        public async Task Format_LongFormatWithStandardSeparator_LegalPersonNoSixteenPrefix()
        {
            var id = OrganisationId.Parse(ValidShort);
            await Assert.That(id.Format(PnrFormat.LongFormatWithStandardSeparator))
                .IsEqualTo("556016-0680");
        }

        [Test]
        public async Task Format_LongFormatWithSeparator_NeverPlus()
        {
            // Orgnummer don't have age semantics — separator is always '-'.
            var id = OrganisationId.Parse(ValidShort);
            var formatted = id.Format(PnrFormat.LongFormatWithSeparator);
            await Assert.That(formatted).Contains("-");
            await Assert.That(formatted).DoesNotContain("+");
        }

        [Test]
        public async Task Format_InvalidEnumValue_ThrowsArgumentOutOfRange()
        {
            var id = OrganisationId.Parse(ValidShort);
            await Assert.That(() => id.Format((PnrFormat)999))
                .Throws<ArgumentOutOfRangeException>();
        }
    }

    public class FromValidatedFactory
    {
        [Test]
        public async Task FromValidated_ValidLegalPerson_ReturnsOrganisationId()
        {
            // 5560160680 is a known-valid legal-person organisationsnummer (form 55).
            var id = OrganisationId.FromValidated("5560160680");

            await Assert.That(id.Form).IsEqualTo(OrganisationForm.EuropakooperativEgtsEric);
            await Assert.That(id.LongFormat()).IsEqualTo("5560160680");
            await Assert.That(id.NumberType).IsEqualTo(OrganisationNumberType.LegalPerson);
        }
    }

    public class EnskildFirma
    {
        // Personnummer-shape Enskild firma orgnummer — month 12, day 12 (a personnummer date).
        private const string EnskildFirmaPersonShape = "191212121212";

        // Samordningsnummer-shape Enskild firma — month 02, day 80 (encoded; real day 20).
        private const string EnskildFirmaCoordShape = "199502809990";

        [Test]
        public async Task PersonnummerShape_Parses()
        {
            var id = OrganisationId.Parse(EnskildFirmaPersonShape);
            await Assert.That(id.NumberType).IsEqualTo(OrganisationNumberType.PhysicalPerson);
            await Assert.That(id.Form).IsEqualTo(OrganisationForm.None);
            await Assert.That(id.ShortFormat()).IsEqualTo("1212121212");
        }

        [Test]
        public async Task SamordningsnummerShape_Parses()
        {
            var id = OrganisationId.Parse(EnskildFirmaCoordShape);
            await Assert.That(id.NumberType).IsEqualTo(OrganisationNumberType.PhysicalPerson);
            await Assert.That(id.Form).IsEqualTo(OrganisationForm.None);
            await Assert.That(id.ShortFormat()).IsEqualTo("9502809990");
        }

        [Test]
        public async Task PlusSeparator_Accepted()
        {
            // Bearer is 100+ years old — '+' is the Swedish convention.
            var id = OrganisationId.Parse("19111230+0007");
            await Assert.That(id).IsNotNull();
            await Assert.That(id.NumberType).IsEqualTo(OrganisationNumberType.PhysicalPerson);
        }

        [Test]
        public async Task SePrefix_Accepted()
        {
            var id = OrganisationId.Parse("SE191112300007");
            await Assert.That(id).IsNotNull();
        }

        [Test]
        public async Task TenDigitForm_Rejected()
        {
            // 10-digit personnummer-shape input has no explicit century — reject as ambiguous.
            await Assert.That(OrganisationId.IsValid("1212121212")).IsFalse();
        }

        [Test]
        public async Task InvalidCenturyForLegal_Rejected()
        {
            // Legal-person 12-digit form requires "16" prefix; "19" is not valid for a legal-person orgnummer.
            await Assert.That(OrganisationId.IsValid("195560160680")).IsFalse();
        }

        [Test]
        public async Task LongFormat_TenDigitsRegardlessOfShape()
        {
            // Per Lag (1974:174) §4, organisationsnummer is always 10 digits.
            // The 12-digit personnummer view of Enskild firma is available via ToPhysicalPersonId().
            var id = OrganisationId.Parse(EnskildFirmaPersonShape);
            await Assert.That(id.LongFormat()).IsEqualTo("1212121212");
        }

        [Test]
        public async Task LongFormat_EqualsShortFormat()
        {
            var id = OrganisationId.Parse(EnskildFirmaPersonShape);
            await Assert.That(id.LongFormat()).IsEqualTo(id.ShortFormat());
        }

        [Test]
        public async Task ToPhysicalPersonId_LongFormat_HasRealCentury()
        {
            // The 12-digit personnummer view IS available via the cross-conversion.
            var org = OrganisationId.Parse(EnskildFirmaPersonShape);
            var person = org.ToPhysicalPersonId();
            await Assert.That(person).IsNotNull();
            await Assert.That(person!.LongFormat()).IsEqualTo("191212121212");
        }

        [Test]
        public async Task ToPhysicalPersonId_EnskildFirma_ReturnsCorrectSubtype()
        {
            // Smoke test for the short-circuit factory path: confirms the result is a
            // PersonalId (not CoordinationId) and decodes the expected BirthDate.
            var org = OrganisationId.Parse(EnskildFirmaPersonShape);
            var person = org.ToPhysicalPersonId();
            await Assert.That(person).IsTypeOf<PersonalId>();
            await Assert.That(person!.BirthDate).IsEqualTo(new DateOnly(1912, 12, 12));
        }

        [Test]
        public async Task ShortFormat_DropsCentury()
        {
            var id = OrganisationId.Parse(EnskildFirmaPersonShape);
            await Assert.That(id.ShortFormat()).IsEqualTo("1212121212");
        }

        [Test]
        public async Task Format_LongFormatWithStandardSeparator_TenDigitsHyphenated()
        {
            var id = OrganisationId.Parse(EnskildFirmaPersonShape);
            await Assert.That(id.Format(PnrFormat.LongFormatWithStandardSeparator))
                .IsEqualTo("121212-1212");
        }

        [Test]
        public async Task Format_LongFormatWithSeparator_PlusForCentenarianEnskildFirma()
        {
            // 191212121212 — born 1912, age 113+ today → '+' separator (Swedish convention)
            var id = OrganisationId.Parse(EnskildFirmaPersonShape);
            var formatted = id.Format(PnrFormat.LongFormatWithSeparator);
            await Assert.That(formatted).IsEqualTo("121212+1212");
        }

        [Test]
        public async Task SixteenPrefixOnPersonShape_Rejected()
        {
            // "161212121212" — synthetic century "16" with personnummer-shape month/day.
            // Must be rejected: no one is born in 1600-something.
            await Assert.That(OrganisationId.IsValid("161212121212")).IsFalse();
        }
    }

    public class TimeProviderFormatting
    {
        // Enskild firma born 1925-06-01 — bears a real personnummer century;
        // hits the centenarian "+" separator on/after 2025-06-01 (Stockholm).
        private const string EnskildFirma19250601 = "192506010004";

        private static FakeTimeProvider StockholmFake(DateTimeOffset utcNow)
        {
            return new FakeTimeProvider(utcNow);
        }

        [Test]
        public async Task Format_LongFormatWithSeparator_BeforeCentenarianBirthday_UsesHyphen()
        {
            // 2025-05-31 12:00 CEST (UTC 10:00) — age 99 in Stockholm, separator '-'.
            var fake = StockholmFake(new DateTimeOffset(2025, 5, 31, 10, 0, 0, TimeSpan.Zero));
            var id = OrganisationId.Parse(EnskildFirma19250601);
            var formatted = id.Format(PnrFormat.LongFormatWithSeparator, fake);

            await Assert.That(formatted).Contains("-");
            await Assert.That(formatted).DoesNotContain("+");
        }

        [Test]
        public async Task Format_LongFormatWithSeparator_OnCentenarianBirthdayStockholm_UsesPlus()
        {
            // 2025-05-31 22:30 UTC = 2025-06-01 00:30 CEST — Stockholm crossed midnight,
            // age 100 in Stockholm, separator '+'.
            var fake = StockholmFake(new DateTimeOffset(2025, 5, 31, 22, 30, 0, TimeSpan.Zero));
            var id = OrganisationId.Parse(EnskildFirma19250601);
            var formatted = id.Format(PnrFormat.LongFormatWithSeparator, fake);

            await Assert.That(formatted).Contains("+");
        }

        [Test]
        public async Task Format_NullTimeProvider_Throws()
        {
            var id = OrganisationId.Parse(EnskildFirma19250601);
            await Assert.That(() => id.Format(PnrFormat.LongFormatWithSeparator, null!))
                .Throws<ArgumentNullException>();
        }

        [Test]
        public async Task Format_LegalPerson_TimeProviderHasNoEffect_AlwaysHyphen()
        {
            // Legal-person orgnummer never use '+' regardless of time provider —
            // the centenarian convention is Enskild-firma-only.
            var fake = StockholmFake(new DateTimeOffset(2025, 5, 31, 22, 30, 0, TimeSpan.Zero));
            var id = OrganisationId.Parse(ValidShort);
            var formatted = id.Format(PnrFormat.LongFormatWithSeparator, fake);

            await Assert.That(formatted).Contains("-");
            await Assert.That(formatted).DoesNotContain("+");
        }
    }

    public class ParsingWithExplicitToday
    {
        // Enskild firma born 1925-06-01 — bears a real personnummer century;
        // hits the centenarian "+" separator on/after 2025-06-01.
        private const string EnskildFirma19250601 = "192506010004";

        [Test]
        public async Task Parse_StringDateOnly_ReturnsValidOrganisationId()
        {
            var today = new DateOnly(2026, 5, 10);
            var id = OrganisationId.Parse(ValidShort, today);
            await Assert.That(id.LongFormat()).IsEqualTo(ValidShort);
        }

        [Test]
        public async Task TryParse_StringDateOnly_TodayIgnored_ProducesSameResultAsZeroArg()
        {
            // OrganisationId.TryParse has no clock dependency; today is API-consistency-only.
            var ok1 = OrganisationId.TryParse(ValidShort, new DateOnly(2026, 5, 10), out var withFar);
            var ok2 = OrganisationId.TryParse(ValidShort, new DateOnly(1900, 1, 1), out var withNear);

            await Assert.That(ok1).IsTrue();
            await Assert.That(ok2).IsTrue();
            await Assert.That(withFar).IsEqualTo(withNear);
        }

        [Test]
        public async Task Parse_SpanDateOnly_EquivalentToStringOverload()
        {
            var today = new DateOnly(2026, 5, 10);
            var fromString = OrganisationId.Parse(ValidShort, today);
            var fromSpan = OrganisationId.Parse(ValidShort.AsSpan(), today);
            await Assert.That(fromSpan).IsEqualTo(fromString);
        }

        [Test]
        public async Task TryParse_SpanDateOnly_EmptyInput_ReturnsFalse()
        {
            var ok = OrganisationId.TryParse(ReadOnlySpan<char>.Empty, new DateOnly(2026, 5, 10), out var result);
            await Assert.That(ok).IsFalse();
            await Assert.That(result).IsNull();
        }

        [Test]
        public async Task Format_LegalPerson_DateOnly_NoSeparatorFlipRegardlessOfDate()
        {
            var legal = OrganisationId.Parse(ValidShort);
            var farFuture = legal.Format(PnrFormat.LongFormatWithSeparator, new DateOnly(2099, 1, 1));
            var farPast = legal.Format(PnrFormat.LongFormatWithSeparator, new DateOnly(1900, 1, 1));
            await Assert.That(farFuture).Contains("-");
            await Assert.That(farFuture).IsEqualTo(farPast);
        }

        [Test]
        public async Task Format_EnskildFirmaCentenarian_DateOnly_BirthdaySeparatorFlip()
        {
            var enskild = OrganisationId.Parse(EnskildFirma19250601);

            var dayBefore = enskild.Format(PnrFormat.LongFormatWithSeparator, new DateOnly(2025, 5, 31));
            var birthday = enskild.Format(PnrFormat.LongFormatWithSeparator, new DateOnly(2025, 6, 1));

            await Assert.That(dayBefore).Contains("-");
            await Assert.That(birthday).Contains("+");
        }
    }

    /// <summary>
    ///     Fixture-driven fan-out mirroring the Java sibling's
    ///     <c>OrganisationIdTest.personalNumbersShouldBeValidOrganisationNumbers</c>.
    ///     Asserts that every valid personnummer also validates and parses as an
    ///     Enskild firma (physical-person) organisation number.
    /// </summary>
    public class FixtureDriven
    {
        public static IEnumerable<string> PersonnummerFinal()
        {
            var rows = CsvLoader.Load("testpersonnummer_final.csv").Skip(1);
            foreach (var r in rows) yield return r[0];
        }

        [Test]
        [MethodDataSource(nameof(PersonnummerFinal))]
        public async Task PersonnummerFinal_ParseAsEnskildFirma(string pin)
        {
            await Assert.That(OrganisationId.IsValid(pin)).IsTrue();
            var id = OrganisationId.Parse(pin);
            await Assert.That(id.NumberType).IsEqualTo(OrganisationNumberType.PhysicalPerson);
        }
    }
}
