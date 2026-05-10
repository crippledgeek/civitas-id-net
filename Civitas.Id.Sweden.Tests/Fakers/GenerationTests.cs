using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Fakers.Internal;

namespace Civitas.Id.Sweden.Tests.Fakers;

public class GenerationTests
{
    public class RandomBirthDate
    {
        [Test]
        public async Task RandomBirthDate_DeterministicSeed_ProducesIdenticalSequence()
        {
            var rngA = new Random(42);
            var rngB = new Random(42);

            var dateA = Generation.RandomBirthDate(rngA.Next);
            var dateB = Generation.RandomBirthDate(rngB.Next);

            await Assert.That(dateA).IsEqualTo(dateB);
        }

        [Test]
        public async Task RandomBirthDate_AlwaysWithinSiblingParityRange()
        {
            var rng = new Random(1);
            for (var i = 0; i < 1000; i++)
            {
                var date = Generation.RandomBirthDate(rng.Next);
                await Assert.That(date.Year).IsGreaterThanOrEqualTo(1970);
                await Assert.That(date.Year).IsLessThan(2020);
            }
        }
    }

    public class RandomGenderDigit
    {
        [Test]
        public async Task RandomGenderDigit_Male_AlwaysOdd()
        {
            var rng = new Random(1);
            for (var i = 0; i < 100; i++)
            {
                var d = Generation.RandomGenderDigit(rng.Next, true);
                await Assert.That(d % 2).IsEqualTo(1);
                await Assert.That(d).IsGreaterThanOrEqualTo(0);
                await Assert.That(d).IsLessThanOrEqualTo(9);
            }
        }

        [Test]
        public async Task RandomGenderDigit_Female_AlwaysEven()
        {
            var rng = new Random(1);
            for (var i = 0; i < 100; i++)
            {
                var d = Generation.RandomGenderDigit(rng.Next, false);
                await Assert.That(d % 2).IsEqualTo(0);
                await Assert.That(d).IsGreaterThanOrEqualTo(0);
                await Assert.That(d).IsLessThanOrEqualTo(9);
            }
        }
    }

    public class BuildPersonnummer
    {
        [Test]
        public async Task BuildPersonnummer_ProducesValidLuhn12Digit()
        {
            var birth = new DateOnly(2008, 5, 8);
            var built = Generation.BuildPersonnummer(birth, 0, 0);

            await Assert.That(built.Length).IsEqualTo(12);
            await Assert.That(PersonalId.IsValid(built)).IsTrue();
        }
    }

    public class BuildSamordningsnummer
    {
        [Test]
        public async Task BuildSamordningsnummer_DayOffsetBy60()
        {
            var birth = new DateOnly(2008, 5, 8);
            var built = Generation.BuildSamordningsnummer(birth, 0, 0);

            await Assert.That(built.Length).IsEqualTo(12);
            await Assert.That(built.Substring(6, 2)).IsEqualTo("68");
            await Assert.That(CoordinationId.IsValid(built)).IsTrue();
        }
    }
}
