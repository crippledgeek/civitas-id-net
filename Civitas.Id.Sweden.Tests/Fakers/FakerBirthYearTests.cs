using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Errors;
using Civitas.Id.Sweden.Fakers;
using Civitas.Id.Sweden.Tests.Helpers;

namespace Civitas.Id.Sweden.Tests.Fakers;

/// <summary>Which birth dates the fakers accept.</summary>
public class FakerBirthYearTests
{
    private const int Seed = 5;
    private const int Seeds = 300;
    private const int YoungestCentenarian = 100;
    private const int OldestCentenarian = 110;

    private static readonly DateOnly FirstAccepted = new(BirthYearCases.FirstAcceptedYear, 1, 1);
    private static readonly DateOnly LastRejected = FirstAccepted.AddDays(-1);

    private static readonly IReadOnlyList<Func<int, IPersonIdFaker<PhysicalPersonId>>> PersonFakers =
        [seed => new PersonalIdFaker(seed), seed => new CoordinationIdFaker(seed)];

    private static readonly IReadOnlyList<Method> Methods =
    [
        new("birthDate", (faker, date) => faker.Generate(date)),
        new("year", (faker, date) => faker.Generate(date.Year, date.Month, date.Day)),
        new("birthDate", (faker, date) => faker.GenerateMale(date)),
        new("year", (faker, date) => faker.GenerateMale(date.Year, date.Month, date.Day)),
        new("birthDate", (faker, date) => faker.GenerateFemale(date)),
        new("year", (faker, date) => faker.GenerateFemale(date.Year, date.Month, date.Day))
    ];

    public class ForADate
    {
        [Test]
        public async Task BeforeTheFirstAcceptedYear_ThrowsWithoutADraw()
        {
            foreach (var create in PersonFakers)
                foreach (var method in Methods)
                    foreach (var date in new[] { DateOnly.MinValue, LastRejected })
                    {
                        var faker = create(Seed);

                        var thrown = await Assert.That(() => { _ = method.Generate(faker, date); })
                            .ThrowsExactly<ArgumentOutOfRangeException>();
                        await Assert.That(thrown!.ParamName).IsEqualTo(method.DateParameter);
                        await UnprintedAssert.AreEqual(faker.Generate(), create(Seed).Generate()).ConfigureAwait(false);
                    }
        }

        /// <summary>No date at all comes before a date that is too early.</summary>
        [Test]
        public async Task ImpossibleComponents_ThrowTheirOwnException()
        {
            foreach (var create in PersonFakers)
            {
                await Assert.That(() => { _ = create(Seed).Generate(0, 1, 1); }).ThrowsExactly<InvalidIdNumberException>();
                await Assert.That(() => { _ = create(Seed).GenerateMale(1700, 2, 29); })
                    .ThrowsExactly<InvalidIdNumberException>();
                await Assert.That(() => { _ = create(Seed).GenerateFemale(1799, 13, 1); })
                    .ThrowsExactly<InvalidIdNumberException>();
            }
        }

        [Test]
        public async Task FromTheFirstAcceptedYear_GeneratesANumberThatParses()
        {
            foreach (var create in PersonFakers)
                foreach (var method in Methods)
                    foreach (var date in new[] { FirstAccepted, DateOnly.MaxValue })
                    {
                        var id = method.Generate(create(Seed), date);

                        await Assert.That(id.BirthDate).IsEqualTo(date);
                        await UnprintedAssert.AreEqual(SwedishOfficialId.ParseAny(id.LongFormat()), id).ConfigureAwait(false);
                    }
        }

        [Test]
        public async Task LegalPerson_AcceptsAnyRegistrationDate()
        {
            foreach (var date in new[] { DateOnly.MinValue, LastRejected })
                await Assert.That(OrganisationId.IsValid(new OrganisationIdFaker(Seed).Generate(date).LongFormat()))
                    .IsTrue();
        }

        /// <summary>Whichever kind the seed would draw.</summary>
        [Test]
        public async Task AnyType_DependsOnTheDateAlone()
        {
            var generated = Enumerable.Range(0, Seeds)
                .Select(seed => new SwedishOfficialIdFaker(seed).Generate(FirstAccepted)).ToList();
            await Assert.That(generated.Select(id => id.GetType()).Distinct().Count()).IsEqualTo(3);
            await Assert.That(generated.IndexesWhere(id => !SwedishOfficialId.IsValid(id.LongFormat()))).IsEmpty();

            foreach (var seed in Enumerable.Range(0, Seeds))
                await Assert.That(() => { _ = new SwedishOfficialIdFaker(seed).Generate(LastRejected); })
                    .ThrowsExactly<ArgumentOutOfRangeException>();
        }
    }

    public class Centenarian
    {
        [Test]
        [Arguments(1)]
        [Arguments(99)]
        [Arguments(1899)]
        public async Task ClockBeforeAnyAcceptedCentenarian_Throws(int year)
        {
            foreach (var create in PersonFakers)
                foreach (var seed in Enumerable.Range(0, Seeds))
                {
                    var thrown = await Assert.That(() => { _ = create(seed).GenerateCentenarian(MidYear.ClockIn(year)); })
                        .ThrowsExactly<ArgumentOutOfRangeException>();
                    await Assert.That(thrown!.ParamName).IsEqualTo("timeProvider");
                }
        }

        /// <summary>
        ///     The ages of the later clock are the ages drawn. Only those that would be too old are lowered.
        /// </summary>
        [Test]
        [Arguments(1900)]
        [Arguments(1905)]
        [Arguments(1909)]
        [Arguments(1910)]
        [Arguments(9999)]
        public async Task Clock_LowersOnlyAnAgeThatReachesBelowTheFirstAcceptedYear(int year)
        {
            const int laterYear = 2026;
            var oldest = Math.Min(OldestCentenarian, year - BirthYearCases.FirstAcceptedYear);
            foreach (var create in PersonFakers)
            {
                var ages = Enumerable.Range(0, Seeds)
                    .Select(seed => year - create(seed).GenerateCentenarian(MidYear.ClockIn(year)).BirthDate.Year).ToList();
                var drawn = Enumerable.Range(0, Seeds)
                    .Select(seed => laterYear - create(seed).GenerateCentenarian(MidYear.ClockIn(laterYear)).BirthDate.Year);

                await Assert.That(ages.Min()).IsEqualTo(YoungestCentenarian);
                await Assert.That(ages.Max()).IsEqualTo(oldest);
                await Assert.That(ages.SequenceEqual(drawn.Select(age => Math.Min(age, oldest)))).IsTrue();
            }
        }
    }

    /// <summary>A method that takes a birth date, and the name of the parameter that an early one is blamed on.</summary>
    private sealed record Method(
        string DateParameter, Func<IPersonIdFaker<PhysicalPersonId>, DateOnly, PhysicalPersonId> Generate);
}
