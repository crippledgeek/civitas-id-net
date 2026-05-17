using System.Globalization;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Errors;
using Civitas.Id.Sweden.TypeConverters;

namespace Civitas.Id.Sweden.Tests.TypeConverters;

/// <summary>
///     Tests for <see cref="OrganisationIdTypeConverter" />.
/// </summary>
public class OrganisationIdTypeConverterTests
{
    private const string ValidLegalPerson = "5560160680"; // 10-digit Aktiebolag
    private const string ValidLegalPersonWithHyphen = "556016-0680";
    private const string ValidEnskildFirma = "191212121212"; // 12-digit personnummer shape

    public class CanConvertFrom
    {
        [Test]
        public async Task ReturnsTrue_ForString()
        {
            var sut = new OrganisationIdTypeConverter();
            await Assert.That(sut.CanConvertFrom(typeof(string))).IsTrue();
        }

        [Test]
        public async Task ReturnsFalse_ForInt()
        {
            var sut = new OrganisationIdTypeConverter();
            await Assert.That(sut.CanConvertFrom(typeof(int))).IsFalse();
        }
    }

    public class CanConvertTo
    {
        [Test]
        public async Task ReturnsTrue_ForString()
        {
            var sut = new OrganisationIdTypeConverter();
            await Assert.That(sut.CanConvertTo(typeof(string))).IsTrue();
        }

        [Test]
        public async Task ReturnsFalse_ForInt()
        {
            var sut = new OrganisationIdTypeConverter();
            await Assert.That(sut.CanConvertTo(typeof(int))).IsFalse();
        }
    }

    public class ConvertFrom
    {
        [Test]
        [Arguments(ValidLegalPerson)]
        [Arguments(ValidLegalPersonWithHyphen)]
        [Arguments(ValidEnskildFirma)]
        public async Task Parses_ValidString(string input)
        {
            var sut = new OrganisationIdTypeConverter();
            var result = sut.ConvertFrom(null, CultureInfo.InvariantCulture, input);
            await Assert.That(result).IsTypeOf<OrganisationId>();
        }

        [Test]
        public async Task Throws_InvalidIdNumberException_OnMalformedString()
        {
            var sut = new OrganisationIdTypeConverter();
            var ex = await Assert.That(() => sut.ConvertFrom(null, CultureInfo.InvariantCulture, "not-an-org"))
                .Throws<InvalidIdNumberException>();
            await Assert.That(ex!.Reason).IsEqualTo(InvalidIdNumberReason.InvalidFormat);
        }

        [Test]
        public async Task Throws_OnEmptyString()
        {
            var sut = new OrganisationIdTypeConverter();
            await Assert.That(() => sut.ConvertFrom(null, CultureInfo.InvariantCulture, ""))
                .Throws<InvalidIdNumberException>();
        }
    }

    public class ConvertTo
    {
        [Test]
        public async Task ReturnsLongFormat_ForLegalPerson()
        {
            // Legal-person LongFormat is the 10-digit form.
            var sut = new OrganisationIdTypeConverter();
            var id = OrganisationId.Parse(ValidLegalPerson);
            var result = sut.ConvertTo(null, CultureInfo.InvariantCulture, id, typeof(string));
            await Assert.That(result).IsEqualTo(ValidLegalPerson);
        }

        [Test]
        public async Task ReturnsLongFormat_ForEnskildFirma()
        {
            // Per Lag (1974:174) §4, organisationsnummer is always 10 digits — including
            // Enskild firma. The 12-digit personnummer view is reached via ToPhysicalPersonId().
            var sut = new OrganisationIdTypeConverter();
            var id = OrganisationId.Parse(ValidEnskildFirma);
            var result = sut.ConvertTo(null, CultureInfo.InvariantCulture, id, typeof(string));
            await Assert.That(result).IsEqualTo("1212121212");
        }

        [Test]
        public async Task RoundTrips_LegalPerson()
        {
            var sut = new OrganisationIdTypeConverter();
            var id = (OrganisationId?)sut.ConvertFrom(null, CultureInfo.InvariantCulture, ValidLegalPerson);
            var roundTripped = sut.ConvertTo(null, CultureInfo.InvariantCulture, id, typeof(string));
            await Assert.That(roundTripped).IsEqualTo(ValidLegalPerson);
        }
    }
}
