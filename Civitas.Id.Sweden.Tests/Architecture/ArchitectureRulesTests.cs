namespace Civitas.Id.Sweden.Tests.Architecture;

using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.TUnit;
using Civitas.Id.Sweden.AspNetCore.Extensions;
using Civitas.Id.Sweden.Core;
using DataAnnotations;
using Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.OpenApi;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

public class ArchitectureRulesTests
{
    private static readonly Architecture Arch = new ArchLoader()
        .LoadAssemblies(
            typeof(PersonalId).Assembly,
            typeof(CivitasIdSwedenJsonContext).Assembly,
            typeof(ValidPersonalIdAttribute).Assembly,
            typeof(CivitasIdSwedenAspNetCoreServiceCollectionExtensions).Assembly)
        .Build();

    public class CorePackage
    {
        [Test]
        public void DoesNotReference_AspNetCore()
        {
            Types().That()
                .ResideInAssembly(typeof(PersonalId).Assembly)
                .Should().NotDependOnAnyTypesThat()
                    .ResideInNamespaceMatching(@"^Microsoft\.AspNetCore(\..*)?$")
                .Because("Core must stay framework-free — consumers may use it from any host")
                .Check(Arch);
        }

        [Test]
        public void DoesNotReference_EntityFrameworkCore()
        {
            Types().That()
                .ResideInAssembly(typeof(PersonalId).Assembly)
                .Should().NotDependOnAnyTypesThat()
                    .ResideInNamespaceMatching(@"^Microsoft\.EntityFrameworkCore(\..*)?$")
                .Because("Core must not pull EF Core — that belongs in a future companion package")
                .Check(Arch);
        }
    }

    public class JsonPackage
    {
        [Test]
        public void DoesNotReference_AspNetCore()
        {
            Types().That()
                .ResideInAssembly(typeof(CivitasIdSwedenJsonContext).Assembly)
                .Should().NotDependOnAnyTypesThat()
                    .ResideInNamespaceMatching(@"^Microsoft\.AspNetCore(\..*)?$")
                .Because(".Json must stay consumable standalone by Blazor WASM, console apps, MAUI")
                .Check(Arch);
        }
    }

    public class DataAnnotationsPackage
    {
        [Test]
        public void DoesNotReference_AspNetCore()
        {
            Types().That()
                .ResideInAssembly(typeof(ValidPersonalIdAttribute).Assembly)
                .Should().NotDependOnAnyTypesThat()
                    .ResideInNamespaceMatching(@"^Microsoft\.AspNetCore(\..*)?$")
                .Because(".DataAnnotations must stay consumable standalone by MAUI, WPF, Avalonia, Blazor")
                .Check(Arch);
        }

        [Test]
        public void DoesNotReference_SystemTextJson()
        {
            Types().That()
                .ResideInAssembly(typeof(ValidPersonalIdAttribute).Assembly)
                .Should().NotDependOnAnyTypesThat()
                    .ResideInNamespaceMatching(@"^System\.Text\.Json(\..*)?$")
                .Because(".DataAnnotations is JSON-serializer-agnostic")
                .Check(Arch);
        }
    }

    public class AspNetCorePackage
    {
        [Test]
        public void IExceptionHandler_Implementations_LiveInAspNetCore()
        {
            Classes().That()
                .ImplementInterface(typeof(IExceptionHandler))
                .Should().ResideInAssembly(typeof(CivitasIdSwedenAspNetCoreServiceCollectionExtensions).Assembly)
                .Because("ASP.NET Core-specific abstractions belong in the .AspNetCore package")
                .Check(Arch);
        }

        [Test]
        public void IOpenApiSchemaTransformer_Implementations_LiveInAspNetCore()
        {
            Classes().That()
                .ImplementInterface(typeof(IOpenApiSchemaTransformer))
                .Should().ResideInAssembly(typeof(CivitasIdSwedenAspNetCoreServiceCollectionExtensions).Assembly)
                .Because("OpenAPI schema transformers are ASP.NET Core-specific")
                .Check(Arch);
        }
    }
}
