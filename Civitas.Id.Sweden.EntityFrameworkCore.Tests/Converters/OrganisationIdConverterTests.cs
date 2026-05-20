using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Errors;
using Civitas.Id.Sweden.Fakers;
using Civitas.Id.Sweden.Format;

namespace Civitas.Id.Sweden.EntityFrameworkCore.Tests.Converters;

public class OrganisationIdConverterTests
{
    public class Construction
    {
        [Test]
        public async Task Construction_NoArguments_BuildsValidConverter()
        {
            var sut = new OrganisationIdConverter();

            await Assert.That(sut).IsNotNull();
            await Assert.That(sut.ProviderClrType).IsEqualTo(typeof(string));
            await Assert.That(sut.ModelClrType).IsEqualTo(typeof(OrganisationId));
        }
    }

    public class Encode
    {
        [Test]
        public async Task ConvertToProvider_LegalPerson_ReturnsTenDigitString()
        {
            // Skatteverket Aktiebolag fixture
            var sut = new OrganisationIdConverter();
            var id = OrganisationId.Parse("5560360793");

            var encoded = (string?)sut.ConvertToProvider(id);

            await Assert.That(encoded).IsEqualTo("5560360793");
            await Assert.That(encoded!.Length).IsEqualTo(10);
        }

        [Test]
        public async Task ConvertToProvider_EnskildFirma_ReturnsTwelveDigitStringWithCentury()
        {
            // 199001019802 is a valid Enskild firma (personnummer shape, day 01 in 1-31 range)
            var sut = new OrganisationIdConverter();
            var id = OrganisationId.Parse("199001019802");

            var encoded = (string?)sut.ConvertToProvider(id);

            await Assert.That(encoded).IsEqualTo("199001019802");
            await Assert.That(encoded!.Length).IsEqualTo(12);
        }
    }

    public class Decode
    {
        [Test]
        public async Task ConvertFromProvider_LegalPersonTenDigit_ReturnsEqualId()
        {
            var sut = new OrganisationIdConverter();
            var original = OrganisationId.Parse("5560360793");

            var encoded = (string?)sut.ConvertToProvider(original);
            var decoded = (OrganisationId?)sut.ConvertFromProvider(encoded);

            await Assert.That(decoded).IsEqualTo(original);
            await Assert.That(decoded!.Form).IsEqualTo(OrganisationForm.AktiebolagOvriga);
        }

        [Test]
        public async Task ConvertFromProvider_EnskildFirmaTwelveDigit_ReturnsEqualId()
        {
            // REGRESSION GUARD: a naive id => id.LongFormat() encode returns only 10 digits
            // for Enskild firma, which drops the century — making re-parse impossible.
            // This test MUST fail against the naive implementation.
            var sut = new OrganisationIdConverter();
            var original = OrganisationId.Parse("199001019802");

            var encoded = (string?)sut.ConvertToProvider(original);
            var decoded = (OrganisationId?)sut.ConvertFromProvider(encoded);

            await Assert.That(decoded).IsEqualTo(original);
            await Assert.That(decoded!.Form).IsEqualTo(OrganisationForm.None);
            await Assert.That(decoded.ToPhysicalPersonId()).IsNotNull();
        }

        [Test]
        public async Task ConvertFromProvider_LuhnTamperedString_Throws()
        {
            var sut = new OrganisationIdConverter();
            // "5560360793" — flip last digit (3 → 4): breaks Luhn
            const string tampered = "5560360794";

            await Assert.That(() => sut.ConvertFromProvider(tampered))
                .Throws<InvalidIdNumberException>();
        }
    }

    public class PrefixHandling
    {
        [Test]
        public async Task ConvertToProvider_PeOrgNrSixteenPrefix_NotReintroducedOnEncode()
        {
            // Parse accepts the legacy "16" prefix (SCB PeOrgNr convention),
            // but output MUST strip it — canonical form is always 10 digits.
            var sut = new OrganisationIdConverter();
            var id = OrganisationId.Parse("165560360793");

            var encoded = (string?)sut.ConvertToProvider(id);

            await Assert.That(encoded).IsEqualTo("5560360793");
            await Assert.That(encoded!.Length).IsEqualTo(10);
        }
    }

    public class MappingHints
    {
        [Test]
        public async Task MappingHints_DeclareSizeTwelveAscii()
        {
            var sut = new OrganisationIdConverter();

            await Assert.That(sut.MappingHints).IsNotNull();
            await Assert.That(sut.MappingHints!.Size).IsEqualTo(12);
            await Assert.That(sut.MappingHints.IsUnicode).IsFalse();
        }
    }

    public class FormMatrix
    {
        public static IEnumerable<(OrganisationForm form, int seed)> FormCases()
        {
            yield return (OrganisationForm.AktiebolagOvriga, 42);
            yield return (OrganisationForm.EkonomiskaForeningar, 42);
            yield return (OrganisationForm.IdeellaForeningar, 42);
            yield return (OrganisationForm.StiftelserFonderOvriga, 42);
            yield return (OrganisationForm.StatligaEnheter, 42);
        }

        [Test]
        [MethodDataSource(nameof(FormCases))]
        public async Task Roundtrip_PreservesForm(OrganisationForm form, int seed)
        {
            var sut = new OrganisationIdConverter();
            var original = new OrganisationIdFaker(seed: seed).Generate(form);

            var encoded = (string?)sut.ConvertToProvider(original);
            var decoded = (OrganisationId?)sut.ConvertFromProvider(encoded);

            await Assert.That(decoded).IsEqualTo(original);
            await Assert.That(decoded!.Form).IsEqualTo(form);
        }
    }
}
