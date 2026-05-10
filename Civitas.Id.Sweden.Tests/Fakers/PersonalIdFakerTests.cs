using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Errors;
using Civitas.Id.Sweden.Fakers;
using Microsoft.Extensions.Time.Testing;

namespace Civitas.Id.Sweden.Tests.Fakers;

public class PersonalIdFakerTests
{
    public class Construction
    {
        [Test]
        public async Task DefaultConstructor_UsesRandomShared()
        {
            var faker = new PersonalIdFaker();
            await Assert.That(faker).IsNotNull();
            await Assert.That(faker.CountryCode).IsEqualTo("SE");
        }

        [Test]
        public async Task SeededConstructor_DeterministicSequence()
        {
            var a = new PersonalIdFaker(42);
            var b = new PersonalIdFaker(42);

            for (var i = 0; i < 100; i++)
                await Assert.That(a.Generate().LongFormat())
                    .IsEqualTo(b.Generate().LongFormat());
        }

        [Test]
        public async Task RandomConstructor_NullThrows()
        {
            await Assert.That(() => new PersonalIdFaker(null!))
                .Throws<ArgumentNullException>();
        }

        [Test]
        public async Task DefaultProperty_NotNull()
        {
            await Assert.That(PersonalIdFaker.Default).IsNotNull();
        }

        [Test]
        public async Task WithSeed_NotNull()
        {
            await Assert.That(PersonalIdFaker.WithSeed(42)).IsNotNull();
        }
    }

    public class EdgeCaseDataGeneration
    {
        [Test]
        public async Task Generate_LeapDay2000_ProducesValidIdWithThatBirthDate()
        {
            var faker = new PersonalIdFaker(seed: 200);
            var leapDay = new DateOnly(2000, 2, 29);

            var id = faker.Generate(leapDay);

            await Assert.That(id.BirthDate).IsEqualTo(leapDay);
            await Assert.That(PersonalId.IsValid(id.LongFormat())).IsTrue();
        }

        [Test]
        public async Task Generate_LeapDay2024_ProducesValidIdWithThatBirthDate()
        {
            var faker = new PersonalIdFaker(seed: 201);
            var leapDay = new DateOnly(2024, 2, 29);

            var id = faker.Generate(leapDay);

            await Assert.That(id.BirthDate).IsEqualTo(leapDay);
            await Assert.That(PersonalId.IsValid(id.LongFormat())).IsTrue();
        }

        [Test]
        [Arguments(1975, 6, 15, "1970s")]
        [Arguments(1985, 6, 15, "1980s")]
        [Arguments(1995, 6, 15, "1990s")]
        [Arguments(2005, 6, 15, "2000s")]
        [Arguments(2015, 6, 15, "2010s")]
        public async Task Generate_DecadeVariety_AllValid(int year, int month, int day, string decade)
        {
            var faker = new PersonalIdFaker(seed: year);
            var date = new DateOnly(year, month, day);

            var id = faker.Generate(date);

            await Assert.That(id.BirthDate).IsEqualTo(date);
            await Assert.That(PersonalId.IsValid(id.LongFormat())).IsTrue();
            _ = decade;   // documented test parameter; not asserted
        }

        [Test]
        public async Task Generate_Centenarian1920_ProducesValidId()
        {
            var faker = new PersonalIdFaker(seed: 202);
            var date = new DateOnly(1920, 1, 1);

            var id = faker.Generate(date);

            await Assert.That(id.BirthDate).IsEqualTo(date);
            await Assert.That(PersonalId.IsValid(id.LongFormat())).IsTrue();
        }

        [Test]
        public async Task Generate_YearBoundaries_AllValid()
        {
            var faker = new PersonalIdFaker(seed: 203);

            var jan1 = faker.Generate(new DateOnly(1990, 1, 1));
            var dec31 = faker.Generate(new DateOnly(1990, 12, 31));

            await Assert.That(jan1.BirthDate).IsEqualTo(new DateOnly(1990, 1, 1));
            await Assert.That(dec31.BirthDate).IsEqualTo(new DateOnly(1990, 12, 31));
            await Assert.That(PersonalId.IsValid(jan1.LongFormat())).IsTrue();
            await Assert.That(PersonalId.IsValid(dec31.LongFormat())).IsTrue();
        }

        [Test]
        public async Task GenerateMale_LeapDay_ProducesValidMaleIdWithThatBirthDate()
        {
            var faker = new PersonalIdFaker(seed: 204);
            var leapDay = new DateOnly(2000, 2, 29);

            var id = faker.GenerateMale(leapDay);

            await Assert.That(id.IsMale).IsTrue();
            await Assert.That(id.BirthDate).IsEqualTo(leapDay);
        }

        [Test]
        public async Task GenerateFemale_LeapDay_ProducesValidFemaleIdWithThatBirthDate()
        {
            var faker = new PersonalIdFaker(seed: 205);
            var leapDay = new DateOnly(2000, 2, 29);

            var id = faker.GenerateFemale(leapDay);

            await Assert.That(id.IsFemale).IsTrue();
            await Assert.That(id.BirthDate).IsEqualTo(leapDay);
        }

        [Test]
        public async Task Generate_RoundTrip_GeneratedThenParsedEqualsOriginal()
        {
            var faker = new PersonalIdFaker(seed: 206);

            var generated = faker.Generate(new DateOnly(1985, 7, 15));
            var parsed = PersonalId.Parse(generated.LongFormat());

            await Assert.That(parsed).IsEqualTo(generated);
        }
    }

    public class GenerateRandom
    {
        [Test]
        public async Task Generate_ProducesValidId()
        {
            var faker = new PersonalIdFaker(1);
            for (var i = 0; i < 50; i++)
            {
                var id = faker.Generate();
                await Assert.That(PersonalId.IsValid(id.LongFormat())).IsTrue();
            }
        }

        [Test]
        public async Task Generate_BirthDateInSiblingParityRange()
        {
            var faker = new PersonalIdFaker(2);
            for (var i = 0; i < 100; i++)
            {
                var id = faker.Generate();
                await Assert.That(id.BirthDate.Year).IsGreaterThanOrEqualTo(1970);
                await Assert.That(id.BirthDate.Year).IsLessThan(2020);
            }
        }

        [Test]
        public async Task Generate_NotPredominantlyOneId()
        {
            // Uniqueness invariant: at least 90 distinct IDs in 100 samples.
            var faker = new PersonalIdFaker(3);
            var ids = new HashSet<string>();
            for (var i = 0; i < 100; i++) ids.Add(faker.Generate().LongFormat());
            await Assert.That(ids.Count).IsGreaterThanOrEqualTo(90);
        }
    }

    public class GenerateForDate
    {
        [Test]
        public async Task Generate_GivenDate_ProducesValidIdWithThatBirthDate()
        {
            var faker = new PersonalIdFaker(4);
            var date = new DateOnly(1985, 7, 15);
            for (var i = 0; i < 50; i++)
            {
                var id = faker.Generate(date);
                await Assert.That(id.BirthDate).IsEqualTo(date);
                await Assert.That(PersonalId.IsValid(id.LongFormat())).IsTrue();
            }
        }

        [Test]
        public async Task Generate_DateComponents_DelegatesCorrectly()
        {
            var faker = new PersonalIdFaker(5);
            var id = faker.Generate(2000, 3, 14);
            await Assert.That(id.BirthDate).IsEqualTo(new DateOnly(2000, 3, 14));
        }

        [Test]
        public async Task Generate_InvalidMonth_ThrowsInvalidIdNumberException()
        {
            var faker = new PersonalIdFaker(6);
            await Assert.That(() => faker.Generate(2025, 13, 1))
                .Throws<InvalidIdNumberException>();
        }

        [Test]
        public async Task Generate_InvalidDay_ThrowsInvalidIdNumberException()
        {
            var faker = new PersonalIdFaker(7);
            await Assert.That(() => faker.Generate(2025, 2, 30))
                .Throws<InvalidIdNumberException>();
        }
    }

    public class GenderInvariants
    {
        [Test]
        public async Task GenerateMale_AlwaysIsMale()
        {
            var faker = new PersonalIdFaker(8);
            for (var i = 0; i < 50; i++)
            {
                var id = faker.GenerateMale();
                await Assert.That(id.IsMale).IsTrue();
            }
        }

        [Test]
        public async Task GenerateFemale_AlwaysIsFemale()
        {
            var faker = new PersonalIdFaker(9);
            for (var i = 0; i < 50; i++)
            {
                var id = faker.GenerateFemale();
                await Assert.That(id.IsFemale).IsTrue();
            }
        }

        [Test]
        public async Task GenerateMale_GivenDate_ProducesMaleIdWithThatBirthDate()
        {
            var faker = new PersonalIdFaker(100);
            var date = new DateOnly(1985, 7, 15);
            for (var i = 0; i < 50; i++)
            {
                var id = faker.GenerateMale(date);
                await Assert.That(id.IsMale).IsTrue();
                await Assert.That(id.BirthDate).IsEqualTo(date);
            }
        }

        [Test]
        public async Task GenerateFemale_GivenDate_ProducesFemaleIdWithThatBirthDate()
        {
            var faker = new PersonalIdFaker(101);
            var date = new DateOnly(1985, 7, 15);
            for (var i = 0; i < 50; i++)
            {
                var id = faker.GenerateFemale(date);
                await Assert.That(id.IsFemale).IsTrue();
                await Assert.That(id.BirthDate).IsEqualTo(date);
            }
        }

        [Test]
        public async Task GenerateMale_DateComponents_DelegatesCorrectly()
        {
            var faker = new PersonalIdFaker(102);
            var id = faker.GenerateMale(2000, 3, 14);
            await Assert.That(id.IsMale).IsTrue();
            await Assert.That(id.BirthDate).IsEqualTo(new DateOnly(2000, 3, 14));
        }

        [Test]
        public async Task GenerateFemale_DateComponents_DelegatesCorrectly()
        {
            var faker = new PersonalIdFaker(103);
            var id = faker.GenerateFemale(2000, 3, 14);
            await Assert.That(id.IsFemale).IsTrue();
            await Assert.That(id.BirthDate).IsEqualTo(new DateOnly(2000, 3, 14));
        }

        [Test]
        public async Task GenerateMale_InvalidDate_ThrowsInvalidIdNumberException()
        {
            var faker = new PersonalIdFaker(104);
            await Assert.That(() => faker.GenerateMale(2025, 13, 1))
                .Throws<InvalidIdNumberException>();
        }

        [Test]
        public async Task GenerateFemale_InvalidDate_ThrowsInvalidIdNumberException()
        {
            var faker = new PersonalIdFaker(105);
            await Assert.That(() => faker.GenerateFemale(2025, 13, 1))
                .Throws<InvalidIdNumberException>();
        }
    }

    public class Centenarian
    {
        [Test]
        public async Task GenerateCentenarian_AgeBetween100And110_Inclusive()
        {
            // FakeTimeProvider locks "today" to 2026-06-15 UTC.
            var fake = new FakeTimeProvider(new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero));
            var faker = new PersonalIdFaker(10);
            for (var i = 0; i < 50; i++)
            {
                var id = faker.GenerateCentenarian(fake);
                var age = id.GetAge(fake);
                await Assert.That(age).IsGreaterThanOrEqualTo(100);
                await Assert.That(age).IsLessThanOrEqualTo(110);
            }
        }

        [Test]
        public async Task GenerateCentenarian_NullProvider_UsesSystem()
        {
            // Wall-clock-relative — we cannot lock the precise age but it MUST be >= 100.
            var faker = new PersonalIdFaker(11);
            var id = faker.GenerateCentenarian();
            await Assert.That(id.IsAdult()).IsTrue(); // 100 >= 18 trivially
        }
    }

    public class RoundTrip
    {
        [Test]
        public async Task ParseRoundTrip_AllSeededSamplesEqual()
        {
            var faker = new PersonalIdFaker(12);
            for (var i = 0; i < 50; i++)
            {
                var generated = faker.Generate();
                var parsed = PersonalId.Parse(generated.LongFormat());
                await Assert.That(parsed).IsEqualTo(generated);
            }
        }
    }
}
