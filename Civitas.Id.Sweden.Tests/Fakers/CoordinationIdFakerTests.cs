using System.Globalization;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Errors;
using Civitas.Id.Sweden.Fakers;
using Microsoft.Extensions.Time.Testing;

namespace Civitas.Id.Sweden.Tests.Fakers;

public class CoordinationIdFakerTests
{
    public class Construction
    {
        [Test]
        public async Task DefaultConstructor_NotNull()
        {
            var faker = new CoordinationIdFaker();
            await Assert.That(faker).IsNotNull();
            await Assert.That(faker.CountryCode).IsEqualTo("SE");
        }

        [Test]
        public async Task SeededConstructor_DeterministicSequence()
        {
            var a = new CoordinationIdFaker(42);
            var b = new CoordinationIdFaker(42);
            for (var i = 0; i < 100; i++)
                await Assert.That(a.Generate().LongFormat()).IsEqualTo(b.Generate().LongFormat());
        }

        [Test]
        public async Task RandomConstructor_NullThrows()
        {
            await Assert.That(() => new CoordinationIdFaker(null!))
                .Throws<ArgumentNullException>();
        }

        [Test]
        public async Task DefaultProperty_NotNull()
        {
            await Assert.That(CoordinationIdFaker.Default).IsNotNull();
        }
    }

    public class EdgeCaseDataGeneration
    {
        [Test]
        public async Task Generate_LeapDay2000_ProducesValidIdWithThatBirthDate()
        {
            var faker = new CoordinationIdFaker(seed: 200);
            var leapDay = new DateOnly(2000, 2, 29);

            var id = faker.Generate(leapDay);

            await Assert.That(id.BirthDate).IsEqualTo(leapDay);
            await Assert.That(CoordinationId.IsValid(id.LongFormat())).IsTrue();
        }

        [Test]
        public async Task Generate_LeapDay2024_ProducesValidIdWithThatBirthDate()
        {
            var faker = new CoordinationIdFaker(seed: 201);
            var leapDay = new DateOnly(2024, 2, 29);

            var id = faker.Generate(leapDay);

            await Assert.That(id.BirthDate).IsEqualTo(leapDay);
            await Assert.That(CoordinationId.IsValid(id.LongFormat())).IsTrue();
        }

        [Test]
        [Arguments(1975, 6, 15, "1970s")]
        [Arguments(1985, 6, 15, "1980s")]
        [Arguments(1995, 6, 15, "1990s")]
        [Arguments(2005, 6, 15, "2000s")]
        [Arguments(2015, 6, 15, "2010s")]
        public async Task Generate_DecadeVariety_AllValid(int year, int month, int day, string decade)
        {
            var faker = new CoordinationIdFaker(seed: year);
            var date = new DateOnly(year, month, day);

            var id = faker.Generate(date);

            await Assert.That(id.BirthDate).IsEqualTo(date);
            await Assert.That(CoordinationId.IsValid(id.LongFormat())).IsTrue();
            _ = decade;   // documented test parameter; not asserted
        }

        [Test]
        public async Task Generate_Centenarian1920_ProducesValidId()
        {
            var faker = new CoordinationIdFaker(seed: 202);
            var date = new DateOnly(1920, 1, 1);

            var id = faker.Generate(date);

            await Assert.That(id.BirthDate).IsEqualTo(date);
            await Assert.That(CoordinationId.IsValid(id.LongFormat())).IsTrue();
        }

        [Test]
        public async Task Generate_YearBoundaries_AllValid()
        {
            var faker = new CoordinationIdFaker(seed: 203);

            var jan1 = faker.Generate(new DateOnly(1990, 1, 1));
            var dec31 = faker.Generate(new DateOnly(1990, 12, 31));

            await Assert.That(jan1.BirthDate).IsEqualTo(new DateOnly(1990, 1, 1));
            await Assert.That(dec31.BirthDate).IsEqualTo(new DateOnly(1990, 12, 31));
            await Assert.That(CoordinationId.IsValid(jan1.LongFormat())).IsTrue();
            await Assert.That(CoordinationId.IsValid(dec31.LongFormat())).IsTrue();
        }

        [Test]
        public async Task GenerateMale_LeapDay_ProducesValidMaleIdWithThatBirthDate()
        {
            var faker = new CoordinationIdFaker(seed: 204);
            var leapDay = new DateOnly(2000, 2, 29);

            var id = faker.GenerateMale(leapDay);

            await Assert.That(id.IsMale).IsTrue();
            await Assert.That(id.BirthDate).IsEqualTo(leapDay);
        }

        [Test]
        public async Task GenerateFemale_LeapDay_ProducesValidFemaleIdWithThatBirthDate()
        {
            var faker = new CoordinationIdFaker(seed: 205);
            var leapDay = new DateOnly(2000, 2, 29);

            var id = faker.GenerateFemale(leapDay);

            await Assert.That(id.IsFemale).IsTrue();
            await Assert.That(id.BirthDate).IsEqualTo(leapDay);
        }

        [Test]
        public async Task Generate_RoundTrip_GeneratedThenParsedEqualsOriginal()
        {
            var faker = new CoordinationIdFaker(seed: 206);

            var generated = faker.Generate(new DateOnly(1985, 7, 15));
            var parsed = CoordinationId.Parse(generated.LongFormat());

            await Assert.That(parsed).IsEqualTo(generated);
        }

        [Test]
        public async Task Generate_LeapDay2000_DayDigitsEncodeAs89()
        {
            var faker = new CoordinationIdFaker(seed: 207);
            var leapDay = new DateOnly(2000, 2, 29);

            var id = faker.Generate(leapDay);

            var longFormat = id.LongFormat();
            var dayDigits = longFormat.AsSpan(6, 2).ToString();
            await Assert.That(dayDigits).IsEqualTo("89");
        }
    }

    public class GenerateRandom
    {
        [Test]
        public async Task Generate_ProducesValidId()
        {
            var faker = new CoordinationIdFaker(1);
            for (var i = 0; i < 50; i++)
            {
                var id = faker.Generate();
                await Assert.That(CoordinationId.IsValid(id.LongFormat())).IsTrue();
            }
        }

        [Test]
        public async Task Generate_DayComponent_InRange61To91()
        {
            // Coordination day-encoding invariant.
            var faker = new CoordinationIdFaker(2);
            for (var i = 0; i < 50; i++)
            {
                var id = faker.Generate();
                var dayDigits = int.Parse(id.LongFormat().AsSpan(6, 2), CultureInfo.InvariantCulture);
                await Assert.That(dayDigits).IsGreaterThanOrEqualTo(61);
                await Assert.That(dayDigits).IsLessThanOrEqualTo(91);
            }
        }

        [Test]
        public async Task Generate_BirthDateInSiblingParityRange()
        {
            var faker = new CoordinationIdFaker(3);
            for (var i = 0; i < 100; i++)
            {
                var id = faker.Generate();
                await Assert.That(id.BirthDate.Year).IsGreaterThanOrEqualTo(1970);
                await Assert.That(id.BirthDate.Year).IsLessThan(2020);
            }
        }

        [Test]
        public async Task Generate_Uniqueness()
        {
            var faker = new CoordinationIdFaker(4);
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
            var faker = new CoordinationIdFaker(5);
            var date = new DateOnly(1985, 7, 15);
            for (var i = 0; i < 50; i++)
            {
                var id = faker.Generate(date);
                await Assert.That(id.BirthDate).IsEqualTo(date);
                await Assert.That(CoordinationId.IsValid(id.LongFormat())).IsTrue();
            }
        }

        [Test]
        public async Task Generate_DateComponents_DelegatesCorrectly()
        {
            var faker = new CoordinationIdFaker(6);
            var id = faker.Generate(2000, 3, 14);
            await Assert.That(id.BirthDate).IsEqualTo(new DateOnly(2000, 3, 14));
        }

        [Test]
        public async Task Generate_InvalidMonth_ThrowsInvalidIdNumberException()
        {
            var faker = new CoordinationIdFaker(7);
            await Assert.That(() => faker.Generate(2025, 13, 1))
                .Throws<InvalidIdNumberException>();
        }

        [Test]
        public async Task Generate_InvalidDay_ThrowsInvalidIdNumberException()
        {
            var faker = new CoordinationIdFaker(8);
            await Assert.That(() => faker.Generate(2025, 2, 30))
                .Throws<InvalidIdNumberException>();
        }
    }

    public class GenderInvariants
    {
        [Test]
        public async Task GenerateMale_AlwaysIsMale()
        {
            var faker = new CoordinationIdFaker(9);
            for (var i = 0; i < 50; i++)
                await Assert.That(faker.GenerateMale().IsMale).IsTrue();
        }

        [Test]
        public async Task GenerateFemale_AlwaysIsFemale()
        {
            var faker = new CoordinationIdFaker(10);
            for (var i = 0; i < 50; i++)
                await Assert.That(faker.GenerateFemale().IsFemale).IsTrue();
        }

        [Test]
        public async Task GenerateMale_GivenDate_ProducesMaleIdWithThatBirthDate()
        {
            var faker = new CoordinationIdFaker(100);
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
            var faker = new CoordinationIdFaker(101);
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
            var faker = new CoordinationIdFaker(102);
            var id = faker.GenerateMale(2000, 3, 14);
            await Assert.That(id.IsMale).IsTrue();
            await Assert.That(id.BirthDate).IsEqualTo(new DateOnly(2000, 3, 14));
        }

        [Test]
        public async Task GenerateFemale_DateComponents_DelegatesCorrectly()
        {
            var faker = new CoordinationIdFaker(103);
            var id = faker.GenerateFemale(2000, 3, 14);
            await Assert.That(id.IsFemale).IsTrue();
            await Assert.That(id.BirthDate).IsEqualTo(new DateOnly(2000, 3, 14));
        }

        [Test]
        public async Task GenerateMale_InvalidDate_ThrowsInvalidIdNumberException()
        {
            var faker = new CoordinationIdFaker(104);
            await Assert.That(() => faker.GenerateMale(2025, 13, 1))
                .Throws<InvalidIdNumberException>();
        }

        [Test]
        public async Task GenerateFemale_InvalidDate_ThrowsInvalidIdNumberException()
        {
            var faker = new CoordinationIdFaker(105);
            await Assert.That(() => faker.GenerateFemale(2025, 13, 1))
                .Throws<InvalidIdNumberException>();
        }
    }

    public class Centenarian
    {
        [Test]
        public async Task GenerateCentenarian_AgeBetween100And110_Inclusive()
        {
            var fake = new FakeTimeProvider(new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero));
            var faker = new CoordinationIdFaker(11);
            for (var i = 0; i < 50; i++)
            {
                var id = faker.GenerateCentenarian(fake);
                var age = id.GetAge(fake);
                await Assert.That(age).IsGreaterThanOrEqualTo(100);
                await Assert.That(age).IsLessThanOrEqualTo(110);
            }
        }
    }

    public class RoundTrip
    {
        [Test]
        public async Task ParseRoundTrip_AllSeededSamplesEqual()
        {
            var faker = new CoordinationIdFaker(12);
            for (var i = 0; i < 50; i++)
            {
                var generated = faker.Generate();
                var parsed = CoordinationId.Parse(generated.LongFormat());
                await Assert.That(parsed).IsEqualTo(generated);
            }
        }
    }
}
