using System.Globalization;
using System.Text;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Fakers;
using Civitas.Id.Sweden.Tests.Helpers;
using Microsoft.Extensions.Time.Testing;

namespace Civitas.Id.Sweden.Tests.Snapshots;

/// <summary>
///     Pins what every seeded faker method returns, as one hash per method, because consumers may rely on
///     a seed giving the same numbers across releases. <c>OrganisationIdFaker.Generate(OrganisationForm)</c>
///     and <c>Generate(OrganisationNumberType)</c> are left out: version 2 removes them.
/// </summary>
public sealed class SeededOutputSnapshotTests
{
    private const int Seeds = 1000;

    /// <summary>More than one call per faker, so that an added or dropped trailing draw is seen too.</summary>
    private const int CallsPerSeed = 3;

    private const int BulkCount = 2;
    private const int PersonMethods = 10;
    private const int OrganisationMethods = 5;
    private const int AnyTypeMethods = 3;

    private static readonly DateOnly LeapDay = new(2000, 2, 29);

    /// <summary>Mid-year, so that the time zone cannot move the date into another year.</summary>
    private static readonly TimeProvider FixedClock =
        new FakeTimeProvider(new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero));

    private static readonly IReadOnlyList<Method> Methods =
    [
        .. PersonMethodsOf(nameof(PersonalIdFaker), seed => new PersonalIdFaker(seed)),
        .. PersonMethodsOf(nameof(CoordinationIdFaker), seed => new CoordinationIdFaker(seed)),
        .. MethodsOf(
            nameof(OrganisationIdFaker),
            seed => new OrganisationIdFaker(seed),
            ("Generate()", f => [f.Generate()]),
            ("Generate(date)", f => [f.Generate(LeapDay)]),
            ("Generate(y,m,d)", f => [f.Generate(LeapDay.Year, LeapDay.Month, LeapDay.Day)]),
            ("GenerateLegalPerson()", f => [f.GenerateLegalPerson()]),
            ("GeneratePhysicalPerson()", f => [f.GeneratePhysicalPerson()])),
        .. MethodsOf(
            nameof(SwedishOfficialIdFaker),
            seed => new SwedishOfficialIdFaker(seed),
            ("Generate()", f => [f.Generate()]),
            ("Generate(date)", f => [f.Generate(LeapDay)]),
            ("Generate(count)", f => f.Generate(BulkCount)))
    ];

    [Test]
    public async Task SeededOutput_IsUnchanged()
    {
        await Assert.That(Methods.Count).IsEqualTo(2 * PersonMethods + OrganisationMethods + AnyTypeMethods);

        var snapshot = new StringBuilder();
        foreach (var (name, run) in Methods)
        {
            var ids = Enumerable.Range(0, Seeds).SelectMany(run).Select(Canonical).ToList();
            await Assert.That(ids.Count).IsGreaterThanOrEqualTo(Seeds * CallsPerSeed);
            var hash = FixtureFiles.Sha256Hex(Encoding.ASCII.GetBytes(string.Join('\n', ids)));
            snapshot.AppendLine(CultureInfo.InvariantCulture, $"{name} = {hash}");
        }

        await Verify(snapshot.ToString()).UseDirectory("VerifiedSnapshots");
    }

    /// <summary>The twelve-digit form where there is one: a sole trader's ten digits drop the century.</summary>
    private static string Canonical(SwedishOfficialId id)
    {
        return (id as OrganisationId)?.ToPhysicalPersonId()?.LongFormat() ?? id.LongFormat();
    }

    private static IEnumerable<Method> PersonMethodsOf(string faker, Func<int, IPersonIdFaker<PhysicalPersonId>> create)
    {
        return MethodsOf(
            faker,
            create,
            ("Generate()", f => [f.Generate()]),
            ("Generate(date)", f => [f.Generate(LeapDay)]),
            ("Generate(y,m,d)", f => [f.Generate(LeapDay.Year, LeapDay.Month, LeapDay.Day)]),
            ("GenerateMale()", f => [f.GenerateMale()]),
            ("GenerateMale(date)", f => [f.GenerateMale(LeapDay)]),
            ("GenerateMale(y,m,d)", f => [f.GenerateMale(LeapDay.Year, LeapDay.Month, LeapDay.Day)]),
            ("GenerateFemale()", f => [f.GenerateFemale()]),
            ("GenerateFemale(date)", f => [f.GenerateFemale(LeapDay)]),
            ("GenerateFemale(y,m,d)", f => [f.GenerateFemale(LeapDay.Year, LeapDay.Month, LeapDay.Day)]),
            ("GenerateCentenarian(clock)", f => [f.GenerateCentenarian(FixedClock)]));
    }

    private static IEnumerable<Method> MethodsOf<TFaker>(
        string faker,
        Func<int, TFaker> create,
        params (string Name, Func<TFaker, IEnumerable<SwedishOfficialId>> Call)[] methods)
    {
        return methods.Select(method => new Method(
            $"{faker}.{method.Name}",
            seed =>
            {
                var instance = create(seed);
                return Enumerable.Range(0, CallsPerSeed).SelectMany(_ => method.Call(instance));
            }));
    }

    /// <summary>One faker method: its label and what a fresh faker with the given seed returns from it.</summary>
    private sealed record Method(string Name, Func<int, IEnumerable<SwedishOfficialId>> Run);
}
