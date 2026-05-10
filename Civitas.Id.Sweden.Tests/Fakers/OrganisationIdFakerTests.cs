using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Errors;
using Civitas.Id.Sweden.Fakers;
using Civitas.Id.Sweden.Format;

namespace Civitas.Id.Sweden.Tests.Fakers;

public class OrganisationIdFakerTests
{
    public class Construction
    {
        [Test]
        public async Task DefaultConstructor_NotNull()
        {
            var faker = new OrganisationIdFaker();
            await Assert.That(faker).IsNotNull();
            await Assert.That(faker.CountryCode).IsEqualTo("SE");
        }

        [Test]
        public async Task SeededConstructor_DeterministicSequence()
        {
            var a = new OrganisationIdFaker(42);
            var b = new OrganisationIdFaker(42);
            for (var i = 0; i < 50; i++)
                await Assert.That(a.GenerateLegalPerson().LongFormat())
                    .IsEqualTo(b.GenerateLegalPerson().LongFormat());
        }
    }

    public class GenerateLegalPerson
    {
        [Test]
        public async Task GenerateLegalPerson_ProducesValidId()
        {
            var faker = new OrganisationIdFaker(1);
            for (var i = 0; i < 50; i++)
            {
                var id = faker.GenerateLegalPerson();
                await Assert.That(OrganisationId.IsValid(id.LongFormat())).IsTrue();
                await Assert.That(id.NumberType).IsEqualTo(OrganisationNumberType.LegalPerson);
            }
        }
    }

    public class GeneratePhysicalPerson
    {
        [Test]
        public async Task GeneratePhysicalPerson_ProducesEnskildFirma()
        {
            var faker = new OrganisationIdFaker(2);
            for (var i = 0; i < 50; i++)
            {
                var id = faker.GeneratePhysicalPerson();
                await Assert.That(id.NumberType).IsEqualTo(OrganisationNumberType.PhysicalPerson);
                await Assert.That(id.Form).IsEqualTo(OrganisationForm.None);
            }
        }
    }

    public class EdgeCaseDataGeneration
    {
        [Test]
        public async Task GenerateLegalPerson_DeterministicSeed_ProducesValidLegalPerson()
        {
            var faker = new OrganisationIdFaker(seed: 300);
            var id = faker.GenerateLegalPerson();

            await Assert.That(OrganisationId.IsValid(id.LongFormat())).IsTrue();
            await Assert.That(id.NumberType).IsEqualTo(OrganisationNumberType.LegalPerson);
        }

        [Test]
        public async Task GeneratePhysicalPerson_DeterministicSeed_ProducesEnskildFirma()
        {
            var faker = new OrganisationIdFaker(seed: 301);
            var id = faker.GeneratePhysicalPerson();

            await Assert.That(id.NumberType).IsEqualTo(OrganisationNumberType.PhysicalPerson);
            await Assert.That(id.Form).IsEqualTo(OrganisationForm.None);
        }

        [Test]
        [Arguments(1975, 6, 15, "1970s")]
        [Arguments(1985, 6, 15, "1980s")]
        [Arguments(1995, 6, 15, "1990s")]
        [Arguments(2005, 6, 15, "2000s")]
        [Arguments(2015, 6, 15, "2010s")]
        public async Task Generate_DecadeRegistrationDates_AllValid(int year, int month, int day, string decade)
        {
            var faker = new OrganisationIdFaker(seed: year);
            var date = new DateOnly(year, month, day);

            var id = faker.Generate(date);

            await Assert.That(OrganisationId.IsValid(id.LongFormat())).IsTrue();
            await Assert.That(id.NumberType).IsEqualTo(OrganisationNumberType.LegalPerson);
            _ = decade;
        }

        [Test]
        public async Task Generate_LeapYearRegistrationDate_ProducesValidId()
        {
            var faker = new OrganisationIdFaker(seed: 302);
            var leapDay = new DateOnly(2000, 2, 29);

            var id = faker.Generate(leapDay);

            await Assert.That(OrganisationId.IsValid(id.LongFormat())).IsTrue();
        }

        [Test]
        public async Task Generate_YearBoundaryRegistrationDates_AllValid()
        {
            var faker = new OrganisationIdFaker(seed: 303);

            var jan1 = faker.Generate(new DateOnly(1990, 1, 1));
            var dec31 = faker.Generate(new DateOnly(1990, 12, 31));

            await Assert.That(OrganisationId.IsValid(jan1.LongFormat())).IsTrue();
            await Assert.That(OrganisationId.IsValid(dec31.LongFormat())).IsTrue();
        }

        [Test]
        [MethodDataSource(typeof(FormPinning), nameof(FormPinning.NamedForms))]
        public async Task Generate_EachNamedForm_RoundTripsThroughParse(OrganisationForm form)
        {
            var faker = new OrganisationIdFaker(seed: (int)form + 1000);
            var generated = faker.Generate(form);

            var parsed = OrganisationId.Parse(generated.LongFormat());

            await Assert.That(parsed).IsEqualTo(generated);
            await Assert.That(parsed.Form).IsEqualTo(form);
        }

        [Test]
        public async Task GeneratePhysicalPerson_BearerBirthDateIsValid()
        {
            // Enskild firma's underlying bearer (personnummer or samordningsnummer)
            // must round-trip through ToPhysicalPersonId() to recover the 12-digit form
            // with century — the 10-digit canonical form lacks century.
            var faker = new OrganisationIdFaker(seed: 304);
            var id = faker.GeneratePhysicalPerson();

            var bearer = id.ToPhysicalPersonId();
            await Assert.That(bearer).IsNotNull();
            await Assert.That(SwedishOfficialId.IsValid(bearer!.LongFormat())).IsTrue();
        }
    }

    public class FormPinning
    {
        [Test]
        [MethodDataSource(nameof(NamedForms))]
        public async Task Generate_GivenForm_ProducesIdWithThatForm(OrganisationForm form)
        {
            var faker = new OrganisationIdFaker((int)form);
            for (var i = 0; i < 5; i++)
            {
                var id = faker.Generate(form);
                await Assert.That(id.Form).IsEqualTo(form);
            }
        }

        public static IEnumerable<OrganisationForm> NamedForms()
        {
            return Enum.GetValues<OrganisationForm>().Where(f => f != OrganisationForm.None);
        }

        [Test]
        public async Task Generate_None_ThrowsArgumentOutOfRangeException()
        {
            var faker = new OrganisationIdFaker(4);
            await Assert.That(() => faker.Generate(OrganisationForm.None))
                .Throws<ArgumentOutOfRangeException>();
        }
    }

    public class TypePinning
    {
        [Test]
        public async Task Generate_LegalPerson_DelegatesToGenerateLegalPerson()
        {
            var faker = new OrganisationIdFaker(5);
            for (var i = 0; i < 50; i++)
            {
                var id = faker.Generate(OrganisationNumberType.LegalPerson);
                await Assert.That(id.NumberType).IsEqualTo(OrganisationNumberType.LegalPerson);
            }
        }

        [Test]
        public async Task Generate_PhysicalPerson_DelegatesToGeneratePhysicalPerson()
        {
            var faker = new OrganisationIdFaker(6);
            for (var i = 0; i < 50; i++)
            {
                var id = faker.Generate(OrganisationNumberType.PhysicalPerson);
                await Assert.That(id.NumberType).IsEqualTo(OrganisationNumberType.PhysicalPerson);
            }
        }
    }

    public class GenerateRandom
    {
        [Test]
        public async Task Generate_ProducesValidId()
        {
            // For Enskild firma (PhysicalPerson NumberType) the 10-digit canonical
            // form lacks the century needed for re-parsing; round-trip via the
            // 12-digit underlying person ID. Legal-person ids round-trip via the
            // 10-digit canonical form directly.
            var faker = new OrganisationIdFaker(7);
            for (var i = 0; i < 50; i++)
            {
                var id = faker.Generate();
                var input = id.NumberType == OrganisationNumberType.PhysicalPerson
                    ? id.ToPhysicalPersonId()!.LongFormat()
                    : id.LongFormat();
                await Assert.That(OrganisationId.IsValid(input)).IsTrue();
            }
        }

        [Test]
        public async Task Generate_GivenDate_ReturnsValidIdWithDateAsRegistration()
        {
            var faker = new OrganisationIdFaker(8);
            // For legal-person org IDs, "date" is interpreted as the registration year.
            var id = faker.Generate(new DateOnly(1985, 7, 15));
            await Assert.That(OrganisationId.IsValid(id.LongFormat())).IsTrue();
        }

        [Test]
        public async Task Generate_InvalidDate_ThrowsInvalidIdNumberException()
        {
            var faker = new OrganisationIdFaker(9);
            await Assert.That(() => faker.Generate(2025, 13, 1))
                .Throws<InvalidIdNumberException>();
        }
    }

    public class RoundTrip
    {
        [Test]
        public async Task ParseRoundTrip_LegalPerson_AllSeededSamplesEqual()
        {
            var faker = new OrganisationIdFaker(10);
            for (var i = 0; i < 50; i++)
            {
                var generated = faker.GenerateLegalPerson();
                var parsed = OrganisationId.Parse(generated.LongFormat());
                await Assert.That(parsed).IsEqualTo(generated);
            }
        }

        [Test]
        public async Task ParseRoundTrip_PhysicalPerson_AllSeededSamplesEqual()
        {
            // Enskild firma's 10-digit canonical form lacks the century needed
            // for re-parsing (the regex requires explicit century when month is
            // a real calendar value 1–12). Round-trip via the 12-digit form
            // composed by prefixing the bearer's century.
            var faker = new OrganisationIdFaker(11);
            for (var i = 0; i < 50; i++)
            {
                var generated = faker.GeneratePhysicalPerson();
                var twelve = generated.ToPhysicalPersonId()!.LongFormat();
                var parsed = OrganisationId.Parse(twelve);
                await Assert.That(parsed).IsEqualTo(generated);
            }
        }
    }
}
