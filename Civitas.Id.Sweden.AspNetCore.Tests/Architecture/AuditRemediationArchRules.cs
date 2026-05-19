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
}
