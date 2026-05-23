namespace Civitas.Id.Sweden.Tests.Architecture;

using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.TUnit;
using Civitas.Id.Sweden.AspNetCore.Extensions;
using Civitas.Id.Sweden.Core;
// ReSharper disable once RedundantNameQualifier — PersonalIdConverter is in this namespace;
// the using is required for unqualified access in typeof() expressions.
using Civitas.Id.Sweden.Dapper;
using Civitas.Id.Sweden.EntityFrameworkCore;
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
            typeof(CivitasIdSwedenAspNetCoreServiceCollectionExtensions).Assembly,
            typeof(PersonalIdConverter).Assembly,
            typeof(PersonalIdHandler).Assembly)
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

        [Test]
        public void DoesNotReference_Dapper()
        {
            Types().That()
                .ResideInAssembly(typeof(PersonalId).Assembly)
                .Should().NotDependOnAnyTypesThat()
                    .ResideInNamespaceMatching(@"^Dapper(\..*)?$")
                .Because("Core must not pull Dapper — that belongs in the Dapper companion package")
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

    public class EntityFrameworkCorePackage
    {
        [Test]
        public void DoesNotReference_InternalNamespace()
        {
            Types().That()
                .ResideInAssembly(typeof(PersonalIdConverter).Assembly)
                .Should().NotDependOnAnyTypesThat()
                    .ResideInNamespaceMatching(@"^Civitas\.Id\.Sweden\.Internal(\..*)?$")
                .Because(".EntityFrameworkCore must consume only the public API surface of the core library")
                .Check(Arch);
        }

        [Test]
        public void DoesNotReference_AspNetCore()
        {
            Types().That()
                .ResideInAssembly(typeof(PersonalIdConverter).Assembly)
                .Should().NotDependOnAnyTypesThat()
                    .ResideInNamespaceMatching(@"^Microsoft\.AspNetCore(\..*)?$")
                .Because(".EntityFrameworkCore must stay usable in non-web hosts (worker services, console apps, MAUI)")
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

    public class DapperPackage
    {
        [Test]
        public void DoesNotReference_AspNetCore()
        {
            Types().That()
                .ResideInAssembly(typeof(PersonalIdHandler).Assembly)
                .Should().NotDependOnAnyTypesThat()
                    .ResideInNamespaceMatching(@"^Microsoft\.AspNetCore(\..*)?$")
                .Because(".Dapper must stay consumable standalone — Dapper is host-agnostic")
                .Check(Arch);
        }

        [Test]
        public void DoesNotReference_EntityFrameworkCore()
        {
            Types().That()
                .ResideInAssembly(typeof(PersonalIdHandler).Assembly)
                .Should().NotDependOnAnyTypesThat()
                    .ResideInNamespaceMatching(@"^Microsoft\.EntityFrameworkCore(\..*)?$")
                .Because(".Dapper must not pull EF Core — these are sibling integration packages")
                .Check(Arch);
        }

        [Test]
        public void DoesNotReference_InternalNamespace()
        {
            Types().That()
                .ResideInAssembly(typeof(PersonalIdHandler).Assembly)
                .Should().NotDependOnAnyTypesThat()
                    .ResideInNamespaceMatching(@"^Civitas\.Id\.Sweden\.Internal(\..*)?$")
                .Because(".Dapper must consume only the public API surface of the core library")
                .Check(Arch);
        }
    }
}
