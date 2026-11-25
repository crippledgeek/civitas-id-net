using System.Globalization;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Errors;
using Civitas.Id.Sweden.Fakers;
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
    // Telefonaktiebolaget LM Ericsson, AktiebolagOvriga
    // (556xxx is the pre-2015 Aktiebolag range — NOT EuropakooperativEgtsEric
    // despite the "55" first-two-digit prefix; see OrganisationId.Form remarks).
    private static readonly string ValidShort = LegalPersonNumbers.Aktiebolag.Digits;
    private static readonly string ValidLong = LegalPersonNumbers.Aktiebolag.WithSixteenPrefix; // 12-digit form
    private static readonly string ValidWithHyphen = LegalPersonNumbers.Aktiebolag.WithHyphen; // hyphen separator

    public class Parsing
    {
        public static IEnumerable<string> IsValidAcceptsKnownValidInputs()
        {
            yield return ValidShort;
            yield return ValidLong;
            yield return ValidWithHyphen;
        }

        [Test]
        [MethodDataSource(nameof(IsValidAcceptsKnownValidInputs))]
        public async Task IsValid_AcceptsKnownValid(string input)
        {
            await Assert.That(OrganisationId.IsValid(input)).IsTrue();
        }

        public static IEnumerable<string?> IsValidRejectsInvalidInputs()
        {
            yield return null;
            yield return "";
            yield return "abc";
            yield return LegalPersonNumbers.Aktiebolag.WithWrongCheckDigit; // wrong checksum
            yield return LegalPersonNumbers.Aktiebolag.WithPersonCentury; // 12-digit form but century is "19", not "16"
        }

        [Test]
        [MethodDataSource(nameof(IsValidRejectsInvalidInputs))]
        public async Task IsValid_RejectsInvalid(string? input)
        {
            await Assert.That(OrganisationId.IsValid(input)).IsFalse();
        }

        public static IEnumerable<string> IsValidAcceptsEnskildFirmaInputs()
        {
            yield return OfficialNumbers.Modern.Long; // personnummer shape
            yield return OfficialNumbers.CentenarianSamordningsnummer.Long; // samordningsnummer shape
        }

        [Test]
        [MethodDataSource(nameof(IsValidAcceptsEnskildFirmaInputs))]
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
            var ok = OrganisationId.TryParse(ValidShort.AsSpan(), CultureInfo.InvariantCulture, out var id);
            await Assert.That(ok).IsTrue();
            await Assert.That(id).IsNotNull();
        }

        [Test]
        public async Task ISpanParsable_Parse_ThrowsOnInvalidSpan()
        {
            await Assert.That(() => OrganisationId.Parse("garbage".AsSpan(), CultureInfo.InvariantCulture))
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
            // Telefonaktiebolaget LM Ericsson. The 556xxx and 559xxx ranges are
            // Aktiebolag (form 49), NOT EuropakooperativEgtsEric — see OrganisationId.Form remarks
            // and the 556/559 regression suite below.
            var id = OrganisationId.Parse(ValidShort);
            await Assert.That(id.Form).IsEqualTo(OrganisationForm.AktiebolagOvriga);
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
            // An Aktiebolag — positions 2-3 = "61" coincidentally falls
            // in the [61,91] samordningsnummer day-offset range, but this is a legal-person
            // orgnummer (parsed without _personCentury). Form must NOT return None.
            var id = OrganisationId.Parse(LegalPersonNumbers.OtherAktiebolag.Digits);
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
            await Assert.That(id.LongFormat()).IsEqualTo(LegalPersonNumbers.Aktiebolag.Digits);
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
                .IsEqualTo(LegalPersonNumbers.Aktiebolag.WithHyphen);
        }

        [Test]
        public async Task Format_LongFormatWithStandardSeparator_LegalPersonNoSixteenPrefix()
        {
            var id = OrganisationId.Parse(ValidShort);
            await Assert.That(id.Format(PnrFormat.LongFormatWithStandardSeparator))
                .IsEqualTo(LegalPersonNumbers.Aktiebolag.WithHyphen);
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
            // Telefonaktiebolaget LM Ericsson. Pre-2015 Aktiebolag (form 49)
            // despite the "55" first-two-digit prefix.
            var id = OrganisationId.FromValidated(LegalPersonNumbers.Aktiebolag.Digits);

            await Assert.That(id.Form).IsEqualTo(OrganisationForm.AktiebolagOvriga);
            await Assert.That(id.LongFormat()).IsEqualTo(LegalPersonNumbers.Aktiebolag.Digits);
            await Assert.That(id.NumberType).IsEqualTo(OrganisationNumberType.LegalPerson);
        }
    }

    /// <summary>
    ///     Regression: <see cref="OrganisationId.Form" /> previously returned
    ///     <see cref="OrganisationForm.EuropakooperativEgtsEric" /> for all orgnummer
    ///     starting with "55", including the pre-2015 Aktiebolag range <c>556…</c>
    ///     and the post-2015 Aktiebolag range <c>559…</c>. Per Bolagsverket's
    ///     2015-01-12 announcement, both sub-ranges are Aktiebolag (form 49).
    /// </summary>
    public class FormRecognises556And559AsAktiebolag
    {
        public static IEnumerable<string> FormFor556And559PrefixIsAktiebolagOvrigaInputs()
        {
            yield return LegalPersonNumbers.Aktiebolag.Digits; // Telefonaktiebolaget LM Ericsson (pre-2015)
            yield return LegalPersonNumbers.OtherAktiebolag.Digits; // Synthetic 556 from fixture row
            yield return LegalPersonNumbers.Aktiebolag559.Digits; // Post-2015 issuance from fixture row
        }

        [Test]
        [MethodDataSource(nameof(FormFor556And559PrefixIsAktiebolagOvrigaInputs))]
        public async Task Form_For_556_And_559_Prefix_Is_AktiebolagOvriga(string orgnr)
        {
            var id = OrganisationId.Parse(orgnr);
            await Assert.That(id.Form).IsEqualTo(OrganisationForm.AktiebolagOvriga);
            await Assert.That(id.NumberType).IsEqualTo(OrganisationNumberType.LegalPerson);
        }

        [Test]
        public async Task Form_For_GenuineEuropakooperativRange_IsEuropakooperativEgtsEric()
        {
            // The faker's third and fourth digits are 21-32, so the number starts with 552 or 553: in the
            // genuine Europakooperativ sub-range, outside the 556/559 Aktiebolag carve-outs.
            var tenDigits = OrganisationIdFaker.WithSeed(1)
                .Generate(OrganisationForm.EuropakooperativEgtsEric).LongFormat();
            await Assert.That(tenDigits.AsSpan(0, 3) is "552" or "553").IsTrue();

            var id = OrganisationId.Parse(tenDigits);
            await Assert.That(id.Form).IsEqualTo(OrganisationForm.EuropakooperativEgtsEric);
            await Assert.That(id.NumberType).IsEqualTo(OrganisationNumberType.LegalPerson);
        }
    }

    public class EnskildFirma
    {
        // Personnummer-shape Enskild firma orgnummer, born 1890-01-01.
        private static readonly string EnskildFirmaPersonShape = OfficialNumbers.Centenarian.Long;

        // Samordningsnummer-shape Enskild firma, born 2000-06-06 (encoded day 66).
        private static readonly string EnskildFirmaCoordShape = OfficialNumbers.ModernSamordningsnummer.Long;

        [Test]
        public async Task PersonnummerShape_Parses()
        {
            var id = OrganisationId.Parse(EnskildFirmaPersonShape);
            await Assert.That(id.NumberType).IsEqualTo(OrganisationNumberType.PhysicalPerson);
            await Assert.That(id.Form).IsEqualTo(OrganisationForm.None);
            await Assert.That(id.ShortFormat()).IsEqualTo(OfficialNumbers.Centenarian.Short);
        }

        [Test]
        public async Task SamordningsnummerShape_Parses()
        {
            var id = OrganisationId.Parse(EnskildFirmaCoordShape);
            await Assert.That(id.NumberType).IsEqualTo(OrganisationNumberType.PhysicalPerson);
            await Assert.That(id.Form).IsEqualTo(OrganisationForm.None);
            await Assert.That(id.ShortFormat()).IsEqualTo(OfficialNumbers.ModernSamordningsnummer.Short);
        }

        [Test]
        public async Task PlusSeparator_Accepted()
        {
            // Bearer is 100+ years old — '+' is the Swedish convention.
            var id = OrganisationId.Parse(OfficialNumbers.Centenarian.LongWithPlus);
            await Assert.That(id).IsNotNull();
            await Assert.That(id.NumberType).IsEqualTo(OrganisationNumberType.PhysicalPerson);
        }

        [Test]
        public async Task SePrefix_Accepted()
        {
            var id = OrganisationId.Parse($"SE{OfficialNumbers.Centenarian.Long}");
            await Assert.That(id).IsNotNull();
        }

        [Test]
        public async Task TenDigitForm_Rejected()
        {
            // 10-digit personnummer-shape input has no explicit century — reject as ambiguous.
            await Assert.That(OrganisationId.IsValid(OfficialNumbers.Centenarian.Short)).IsFalse();
        }

        [Test]
        public async Task InvalidCenturyForLegal_Rejected()
        {
            // Legal-person 12-digit form requires "16" prefix; "19" is not valid for a legal-person orgnummer.
            await Assert.That(OrganisationId.IsValid(LegalPersonNumbers.Aktiebolag.WithPersonCentury)).IsFalse();
        }

        [Test]
        public async Task LongFormat_TenDigitsRegardlessOfShape()
        {
            // Per Lag (1974:174) §4, organisationsnummer is always 10 digits.
            // The 12-digit personnummer view of Enskild firma is available via ToPhysicalPersonId().
            var id = OrganisationId.Parse(EnskildFirmaPersonShape);
            await Assert.That(id.LongFormat()).IsEqualTo(OfficialNumbers.Centenarian.Short);
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
            await Assert.That(person!.LongFormat()).IsEqualTo(OfficialNumbers.Centenarian.Long);
        }

        [Test]
        public async Task ToPhysicalPersonId_EnskildFirma_ReturnsCorrectSubtype()
        {
            // Smoke test for the short-circuit factory path: confirms the result is a
            // PersonalId (not CoordinationId) and decodes the expected BirthDate.
            var org = OrganisationId.Parse(EnskildFirmaPersonShape);
            var person = org.ToPhysicalPersonId();
            await Assert.That(person).IsTypeOf<PersonalId>();
            await Assert.That(person!.BirthDate).IsEqualTo(new DateOnly(1890, 1, 1));
        }

        [Test]
        public async Task ShortFormat_DropsCentury()
        {
            var id = OrganisationId.Parse(EnskildFirmaPersonShape);
            await Assert.That(id.ShortFormat()).IsEqualTo(OfficialNumbers.Centenarian.Short);
        }

        [Test]
        public async Task Format_LongFormatWithStandardSeparator_TenDigitsHyphenated()
        {
            var id = OrganisationId.Parse(EnskildFirmaPersonShape);
            await Assert.That(id.Format(PnrFormat.LongFormatWithStandardSeparator))
                .IsEqualTo(OfficialNumbers.Centenarian.ShortWithHyphen);
        }

        [Test]
        public async Task Format_LongFormatWithSeparator_PlusForCentenarianEnskildFirma()
        {
            // Born 1890, age 136+ today → '+' separator (Swedish convention)
            var id = OrganisationId.Parse(EnskildFirmaPersonShape);
            var formatted = id.Format(PnrFormat.LongFormatWithSeparator);
            await Assert.That(formatted).IsEqualTo(OfficialNumbers.Centenarian.ShortWithPlus);
        }

        [Test]
        public async Task SixteenPrefixOnPersonShape_Rejected()
        {
            // Century "16" with personnummer-shape month/day.
            // Must be rejected: no one is born in 1600-something.
            await Assert.That(OrganisationId.IsValid($"16{OfficialNumbers.Centenarian.Short}")).IsFalse();
        }
    }

    public class TimeProviderFormatting
    {
        // Enskild firma born 1895-06-01 — bears a real personnummer century;
        // hits the centenarian "+" separator on/after 1995-06-01 (Stockholm).
        private static readonly string EnskildFirmaHundredIn1995 = OfficialNumbers.HundredIn1995.Long;

        private static FakeTimeProvider StockholmFake(DateTimeOffset utcNow)
        {
            return new FakeTimeProvider(utcNow);
        }

        [Test]
        public async Task Format_LongFormatWithSeparator_BeforeCentenarianBirthday_UsesHyphen()
        {
            // 1995-05-31 12:00 CEST (UTC 10:00) — age 99 in Stockholm, separator '-'.
            var fake = StockholmFake(new DateTimeOffset(1995, 5, 31, 10, 0, 0, TimeSpan.Zero));
            var id = OrganisationId.Parse(EnskildFirmaHundredIn1995);
            var formatted = id.Format(PnrFormat.LongFormatWithSeparator, fake);

            await Assert.That(formatted).Contains("-");
            await Assert.That(formatted).DoesNotContain("+");
        }

        [Test]
        public async Task Format_LongFormatWithSeparator_OnCentenarianBirthdayStockholm_UsesPlus()
        {
            // 1995-05-31 22:30 UTC = 1995-06-01 00:30 CEST — Stockholm crossed midnight,
            // age 100 in Stockholm, separator '+'.
            var fake = StockholmFake(new DateTimeOffset(1995, 5, 31, 22, 30, 0, TimeSpan.Zero));
            var id = OrganisationId.Parse(EnskildFirmaHundredIn1995);
            var formatted = id.Format(PnrFormat.LongFormatWithSeparator, fake);

            await Assert.That(formatted).Contains("+");
        }

        [Test]
        public async Task Format_NullTimeProvider_Throws()
        {
            var id = OrganisationId.Parse(EnskildFirmaHundredIn1995);
            await Assert.That(() => id.Format(PnrFormat.LongFormatWithSeparator, null!))
                .Throws<ArgumentNullException>();
        }

        [Test]
        public async Task Format_LegalPerson_TimeProviderHasNoEffect_AlwaysHyphen()
        {
            // Legal-person orgnummer never use '+' regardless of time provider —
            // the centenarian convention is Enskild-firma-only.
            var fake = StockholmFake(new DateTimeOffset(1995, 5, 31, 22, 30, 0, TimeSpan.Zero));
            var id = OrganisationId.Parse(ValidShort);
            var formatted = id.Format(PnrFormat.LongFormatWithSeparator, fake);

            await Assert.That(formatted).Contains("-");
            await Assert.That(formatted).DoesNotContain("+");
        }
    }

    public class ParsingWithExplicitToday
    {
        // Enskild firma born 1895-06-01 — bears a real personnummer century;
        // hits the centenarian "+" separator on/after 1995-06-01.
        private static readonly string EnskildFirmaHundredIn1995 = OfficialNumbers.HundredIn1995.Long;

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
            var enskild = OrganisationId.Parse(EnskildFirmaHundredIn1995);

            var dayBefore = enskild.Format(PnrFormat.LongFormatWithSeparator, new DateOnly(1995, 5, 31));
            var birthday = enskild.Format(PnrFormat.LongFormatWithSeparator, new DateOnly(1995, 6, 1));

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
            return CsvLoader.Column(Fixtures.PersonnummerFinal);
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
