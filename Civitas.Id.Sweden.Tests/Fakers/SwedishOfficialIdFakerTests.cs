using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Fakers;
using Civitas.Id.Sweden.Format;

namespace Civitas.Id.Sweden.Tests.Fakers;

public class SwedishOfficialIdFakerTests
{
    public class Construction
    {
        [Test]
        public async Task Default_NotNull()
        {
            await Assert.That(SwedishOfficialIdFaker.Default).IsNotNull();
        }

        [Test]
        public async Task SeededConstructor_Deterministic()
        {
            var a = new SwedishOfficialIdFaker(42);
            var b = new SwedishOfficialIdFaker(42);
            for (var i = 0; i < 50; i++)
                await Assert.That(a.Generate().LongFormat())
                    .IsEqualTo(b.Generate().LongFormat());
        }
    }

    public class GenerateSingle
    {
        [Test]
        public async Task Generate_ProducesValidSwedishOfficialId()
        {
            var faker = new SwedishOfficialIdFaker(1);
            for (var i = 0; i < 50; i++)
            {
                var id = faker.Generate();
                await Assert.That(SwedishOfficialId.IsValid(id.LongFormat())).IsTrue();
            }
        }

        [Test]
        public async Task Generate_TypeDistribution_AllThreeAppearIn300Samples()
        {
            var faker = new SwedishOfficialIdFaker(2);
            var hasPersonal = false;
            var hasCoord = false;
            var hasOrg = false;
            for (var i = 0; i < 300; i++)
                switch (faker.Generate())
                {
                    case PersonalId: hasPersonal = true; break;
                    case CoordinationId: hasCoord = true; break;
                    case OrganisationId: hasOrg = true; break;
                }

            await Assert.That(hasPersonal).IsTrue();
            await Assert.That(hasCoord).IsTrue();
            await Assert.That(hasOrg).IsTrue();
        }
    }

    public class EdgeCaseDataGeneration
    {
        [Test]
        public async Task Generate_LeapYearDate_ProducesValidIdAcrossSubtypes()
        {
            var faker = new SwedishOfficialIdFaker(seed: 400);
            var leapDay = new DateOnly(2000, 2, 29);

            var id = faker.Generate(leapDay);

            await Assert.That(SwedishOfficialId.IsValid(id.LongFormat())).IsTrue();
        }

        [Test]
        [Arguments(1975, 6, 15)]
        [Arguments(1985, 6, 15)]
        [Arguments(1995, 6, 15)]
        [Arguments(2005, 6, 15)]
        [Arguments(2015, 6, 15)]
        public async Task Generate_DecadeDates_AllValid(int year, int month, int day)
        {
            var faker = new SwedishOfficialIdFaker(seed: year);
            var date = new DateOnly(year, month, day);

            var id = faker.Generate(date);

            await Assert.That(SwedishOfficialId.IsValid(id.LongFormat())).IsTrue();
        }

        [Test]
        public async Task Generate_BulkOfTwoHundred_AllValid()
        {
            var faker = new SwedishOfficialIdFaker(seed: 401);

            var ids = faker.Generate(200);

            await Assert.That(ids.Count).IsEqualTo(200);
            foreach (var id in ids)
                await Assert.That(SwedishOfficialId.IsValid(id.LongFormat())).IsTrue();
        }

        [Test]
        public async Task Generate_BulkOfTwoHundred_ContainsAllSubtypes()
        {
            var faker = new SwedishOfficialIdFaker(seed: 402);

            var ids = faker.Generate(200);

            var hasPersonal = ids.Any(id => id is PersonalId);
            var hasCoord = ids.Any(id => id is CoordinationId);
            var hasOrg = ids.Any(id => id is OrganisationId);

            await Assert.That(hasPersonal).IsTrue();
            await Assert.That(hasCoord).IsTrue();
            await Assert.That(hasOrg).IsTrue();
        }

        [Test]
        public async Task Generate_RoundTrip_AllSubtypesParseable()
        {
            var faker = new SwedishOfficialIdFaker(seed: 403);

            for (var i = 0; i < 50; i++)
            {
                var generated = faker.Generate();

                // For Enskild firma OrganisationId, LongFormat is 10-digit and not
                // round-trippable directly through ParseAny; skip that subtype's
                // round-trip check.
                if (generated is OrganisationId { NumberType: OrganisationNumberType.PhysicalPerson })
                    continue;

                var parsed = SwedishOfficialId.ParseAny(generated.LongFormat());
                await Assert.That(parsed).IsEqualTo(generated);
            }
        }

        [Test]
        public async Task Generate_YearBoundaryDates_AllValid()
        {
            var faker = new SwedishOfficialIdFaker(seed: 404);

            var jan1 = faker.Generate(new DateOnly(1990, 1, 1));
            var dec31 = faker.Generate(new DateOnly(1990, 12, 31));

            await Assert.That(SwedishOfficialId.IsValid(jan1.LongFormat())).IsTrue();
            await Assert.That(SwedishOfficialId.IsValid(dec31.LongFormat())).IsTrue();
        }
    }

    public class GenerateBulk
    {
        [Test]
        [Arguments(0)]
        [Arguments(1)]
        [Arguments(10)]
        [Arguments(1000)]
        public async Task Generate_Count_ReturnsExactCount(int count)
        {
            var faker = new SwedishOfficialIdFaker(count);
            var list = faker.Generate(count);
            await Assert.That(list.Count).IsEqualTo(count);
        }

        [Test]
        public async Task Generate_NegativeCount_Throws()
        {
            var faker = new SwedishOfficialIdFaker(1);
            await Assert.That(() => faker.Generate(-1))
                .Throws<ArgumentOutOfRangeException>();
        }

        [Test]
        public async Task Generate_BulkResult_AllValid()
        {
            var faker = new SwedishOfficialIdFaker(3);
            var list = faker.Generate(100);
            foreach (var id in list)
                await Assert.That(SwedishOfficialId.IsValid(id.LongFormat())).IsTrue();
        }
    }

    public class GenerateForDate
    {
        [Test]
        public async Task Generate_GivenDate_ReturnsValidId()
        {
            var faker = new SwedishOfficialIdFaker(4);
            var id = faker.Generate(new DateOnly(1985, 7, 15));
            await Assert.That(SwedishOfficialId.IsValid(id.LongFormat())).IsTrue();
        }
    }
}
