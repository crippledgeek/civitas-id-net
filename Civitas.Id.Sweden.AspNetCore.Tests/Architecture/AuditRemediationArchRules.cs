using ArchUnitNET.Loader;
using ArchUnitNET.TUnit;
using Civitas.Id.Sweden.AspNetCore.Extensions;
using Microsoft.AspNetCore.Diagnostics;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using ArchUnitArchitecture = ArchUnitNET.Domain.Architecture;

namespace Civitas.Id.Sweden.AspNetCore.Tests.Architecture;

/// <summary>
/// Cross-cutting structural invariants locked in by the audit remediation
/// (Findings 1–9 + Gaps A–D). Failures here indicate the audit-remediation
/// posture has been silently regressed.
///
/// <para>
/// Both name-based AND interface-based assertions are kept side-by-side
/// (per follow-up #30):
/// </para>
/// <list type="bullet">
/// <item>
/// <b>Name-based</b>: pins THIS specific class to the rule — readable, but
/// fragile to renames.
/// </item>
/// <item>
/// <b>Interface-based</b>: catches ANY future implementation of the
/// contract — broader regression net. If a rename drops the name-based
/// assertion, the interface-based one still asserts the property on the
/// new name.
/// </item>
/// </list>
/// </summary>
public sealed class AuditRemediationArchRules
{
    private static readonly ArchUnitArchitecture Arch = new ArchLoader()
        .LoadAssemblies(typeof(CivitasIdSwedenAspNetCoreServiceCollectionExtensions).Assembly)
        .Build();

    [Test]
    public void BothJsonOptionsSetups_AreSealedInternalClasses()
    {
        Classes()
            .That().HaveNameContaining("JsonOptionsSetup")
            .Should().BeSealed().AndShould().NotBePublic()
            .Because("JSON option-setup adapters are framework-internal — public surface is the AddCivitasIdSwedenAspNetCore extension")
            .Check(Arch);
    }

    [Test]
    public void InvalidIdNumberExceptionHandler_Implements_IExceptionHandler()
    {
        Classes()
            .That().HaveName("InvalidIdNumberExceptionHandler")
            .Should().ImplementInterface(typeof(IExceptionHandler))
            .Because("The library's PNR exception-to-ProblemDetails bridge must use the IExceptionHandler contract")
            .Check(Arch);
    }

    [Test]
    public void StartupValidator_Implements_IHostedService()
    {
        Classes()
            .That().HaveName("CivitasIdSwedenStartupValidator")
            .Should().ImplementInterface(typeof(IHostedService))
            .Because("Startup-time validator runs as an IHostedService so failures surface during app start, not first request")
            .Check(Arch);
    }

    [Test]
    public void Any_IExceptionHandler_Implementation_IsSealedInternal()
    {
        Classes()
            .That().ImplementInterface(typeof(IExceptionHandler))
            .Should().BeSealed().AndShould().NotBePublic()
            .Because("All IExceptionHandler implementations in this assembly are framework-internal exception bridges — public extension surface is the AddCivitasIdSwedenAspNetCore registration")
            .Check(Arch);
    }

    [Test]
    public void Any_IHostedService_Implementation_IsSealedInternal()
    {
        Classes()
            .That().ImplementInterface(typeof(IHostedService))
            .Should().BeSealed().AndShould().NotBePublic()
            .Because("All IHostedService implementations in this assembly are framework-internal startup validators — public extension surface is the AddCivitasIdSwedenAspNetCore registration")
            .Check(Arch);
    }
}
