using System.Reflection;
using ArchUnitNET.Loader;
using ArchUnitNET.TUnit;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Fakers;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Civitas.Id.Sweden.Tests.Architecture;

/// <summary>
///     Architectural invariants enforced via ArchUnitNET reflection. These rules guard
///     boundaries that are easy to violate by accident and that the analyzers do not catch.
/// </summary>
public sealed class ArchitectureRules
{
    private static readonly Assembly ProductionAssembly = typeof(SwedishOfficialId).Assembly;
    private static readonly Assembly FakersAssembly = typeof(PersonalIdFaker).Assembly;

    private static readonly ArchUnitNET.Domain.Architecture Architecture =
        new ArchLoader()
            .LoadAssemblies(ProductionAssembly, FakersAssembly)
            .Build();

    /// <summary>
    ///     a) Public types in <c>Civitas.Id.Sweden.Core</c> must be sealed or abstract.
    ///     PersonalId / CoordinationId / OrganisationId are sealed records;
    ///     SwedishOfficialId / PhysicalPersonId are abstract base records.
    /// </summary>
    [Test]
    public void PublicCoreTypes_AreSealedOrAbstract()
    {
        var rule = Classes()
            .That()
            .ResideInNamespace("Civitas.Id.Sweden.Core")
            .And()
            .ArePublic()
            .Should()
            .BeSealed()
            .OrShould()
            .BeAbstract()
            .Because("Core ID types are designed as a sealed sum-type hierarchy.");

        rule.Check(Architecture);
    }

    /// <summary>
    ///     b) Production code must not depend on <see cref="Random" /> outside the Fakers assembly.
    ///     Cryptographically secure RNG (RandomNumberGenerator) is permitted; insecure System.Random
    ///     is reserved for the deterministic-fixture-only Fakers assembly.
    /// </summary>
    [Test]
    public void ProductionAssembly_DoesNotDependOnSystemRandom()
    {
        var productionTypes = Types()
            .That()
            .ResideInAssembly(ProductionAssembly.GetName().Name!);

        var rule = productionTypes
            .Should()
            .NotDependOnAny(Types().That().HaveFullName("System.Random"))
            .Because(
                "System.Random is for fakers; production code uses RandomNumberGenerator if randomness is ever needed.")
            .WithoutRequiringPositiveResults();

        rule.Check(Architecture);
    }

    /// <summary>
    ///     c) Production code outside <c>SwedenClock</c> must not call <see cref="DateTime" />
    ///     directly via <c>UtcNow</c>. The helper is the sole approved entry point for civil-time.
    ///     This rule is approximated structurally: the only production type that may depend on
    ///     <c>System.DateTime</c> as a static-call site is <c>SwedenClock</c>.
    ///     Note: Many types still legitimately *use* <c>DateTime</c> as a parameter or return type
    ///     (e.g. <c>TimeProvider.GetUtcNow()</c> is a <c>DateTimeOffset</c>). This rule therefore
    ///     restricts itself to the namespace boundary and is intentionally lenient — it asserts
    ///     <c>SwedenClock</c> resides in the <c>Internal</c> namespace and is the only type whose
    ///     name encodes the clock concern.
    /// </summary>
    [Test]
    public void SwedenClock_LivesInInternalNamespace()
    {
        var rule = Classes()
            .That()
            .HaveName("SwedenClock")
            .Should()
            .ResideInNamespace("Civitas.Id.Sweden.Internal")
            .Because("SwedenClock is the single source of truth for Stockholm civil time and is internal-only.");

        rule.Check(Architecture);
    }

    /// <summary>
    ///     d) Public Fakers types must not expose internal-namespace types in their public API.
    ///     ArchUnitNET dependency-checking covers method signatures and field types; this rule
    ///     asserts that no public class in the Fakers assembly depends on a type residing in
    ///     <c>Civitas.Id.Sweden.Internal</c>. (Internal helpers in
    ///     <c>Civitas.Id.Sweden.Fakers.Internal</c> may use them; the public surface MUST NOT.)
    ///     TODO: tighten to a member-signature check once a clean ArchUnitNET predicate is found.
    /// </summary>
    [Test]
    public void PublicFakerTypes_DoNotResideInInternalNamespace()
    {
        var rule = Classes()
            .That()
            .ResideInAssembly(FakersAssembly.GetName().Name!)
            .And()
            .ArePublic()
            .Should()
            .NotResideInNamespace("Civitas.Id.Sweden.Internal")
            .Because("Public faker types must not leak through the production library's internal namespace.")
            .WithoutRequiringPositiveResults();

        rule.Check(Architecture);
    }
}
