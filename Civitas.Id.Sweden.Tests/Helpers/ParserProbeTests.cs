using System.Reflection;
using Civitas.Id.Sweden.Core;

namespace Civitas.Id.Sweden.Tests.Helpers;

public class ParserProbeTests
{
    private static readonly IReadOnlyList<Type> ParsedTypes =
        [typeof(PersonalId), typeof(CoordinationId), typeof(OrganisationId), typeof(SwedishOfficialId)];

    /// <summary>
    ///     Counted from the types, so that a new entry point cannot be forgotten. The string form of
    ///     <see cref="IParsable{TSelf}" /> is implemented explicitly, so it is not among the public methods.
    /// </summary>
    [Test]
    public async Task AsksEveryEntryPointThatDoesNotThrow()
    {
        const BindingFlags declaredStatic = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;
        var publicMethods = ParsedTypes.SelectMany(type => type.GetMethods(declaredStatic))
            .Count(method => method.Name is "TryParse" or "TryParseAny" or "IsValid");
        var explicitMethods = ParsedTypes.Count(type => type.GetInterfaces().Any(contract =>
            contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IParsable<>)));

        await Assert.That(explicitMethods).IsEqualTo(3);
        await Assert.That(ParserProbe.Count).IsEqualTo(publicMethods + explicitMethods);
    }

    [Test]
    public async Task AcceptedBy_OfficialNumber_HoldsItsOwnEntryPointsOnly()
    {
        var accepted = ParserProbe.AcceptedBy(LegalPersonNumbers.Aktiebolag.Digits);

        await Assert.That(accepted.Count(name => name.StartsWith("OrganisationId/", StringComparison.Ordinal))).IsEqualTo(7);
        await Assert.That(accepted.Count(name => name.StartsWith("SwedishOfficialId/", StringComparison.Ordinal))).IsEqualTo(5);
        await Assert.That(accepted.Count).IsEqualTo(12);
    }

    [Test]
    public async Task AcceptedBy_NoNumber_IsEmpty()
    {
        await Assert.That(ParserProbe.AcceptedBy("not-an-id")).IsEmpty();
    }
}
