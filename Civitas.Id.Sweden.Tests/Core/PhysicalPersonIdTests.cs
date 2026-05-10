using System.Globalization;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Tests.Helpers;

namespace Civitas.Id.Sweden.Tests.Core;

/// <summary>
///     Tests for the abstract base <see cref="PhysicalPersonId" />, exercised
///     through its concrete subtypes (<see cref="PersonalId" /> and
///     <see cref="CoordinationId" />). Mirrors the Java sibling's
///     <c>PersonOfficialIdTest</c> fixture-driven fan-out.
/// </summary>
public class PhysicalPersonIdTests
{
    /// <summary>
    ///     Reference "today" for personnummer age assertions, matching the Java
    ///     sibling's fixed Clock at 2025-04-23.
    /// </summary>
    private static readonly DateOnly PersonnummerFixtureToday = new(2025, 4, 23);

    /// <summary>
    ///     Fixture-driven fan-out mirroring
    ///     <c>PersonOfficialIdTest.shouldBeAValidSwedishPersonalIdWithCorrectAgeAndGender</c>.
    ///     Routes assertions through the <see cref="PhysicalPersonId" /> base type
    ///     (gender + age semantics) rather than the concrete subtype API.
    /// </summary>
    public class FixtureDriven
    {
        public static IEnumerable<(string Pin, bool Female, bool Male, bool Adult, bool Child, int Age, bool Valid)>
            PersonnummerFinal()
        {
            var rows = CsvLoader.Load("testpersonnummer_final.csv").Skip(1);
            foreach (var r in rows)
                yield return (
                    r[0],
                    bool.Parse(r[1]),
                    bool.Parse(r[2]),
                    bool.Parse(r[3]),
                    bool.Parse(r[4]),
                    int.Parse(r[5], CultureInfo.InvariantCulture),
                    bool.Parse(r[6]));
        }

        [Test]
        [MethodDataSource(nameof(PersonnummerFinal))]
        public async Task PersonnummerFinal_AsPhysicalPersonId_HasCorrectAgeAndGender(
            string pin, bool female, bool male, bool adult, bool child, int age, bool valid)
        {
            await Assert.That(SwedishOfficialId.IsValid(pin)).IsEqualTo(valid);
            if (!valid) return;

            // Java parses as PersonalId or falls back to CoordinationId.
            // Route through the base type so the test exercises PhysicalPersonId.
            PhysicalPersonId id = PersonalId.IsValid(pin)
                ? PersonalId.Parse(pin)
                : CoordinationId.Parse(pin);

            await Assert.That(id.IsFemale).IsEqualTo(female);
            await Assert.That(id.IsMale).IsEqualTo(male);
            await Assert.That(id.GetAge(PersonnummerFixtureToday)).IsEqualTo(age);
            await Assert.That(id.IsAdult(PersonnummerFixtureToday)).IsEqualTo(adult);
            await Assert.That(id.IsChild(PersonnummerFixtureToday)).IsEqualTo(child);
        }
    }
}
