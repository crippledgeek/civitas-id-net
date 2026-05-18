using System.Diagnostics.CodeAnalysis;
using Civitas.Id.Sweden.AspNetCore.ExceptionHandlers;
using Civitas.Id.Sweden.AspNetCore.Json;
using Civitas.Id.Sweden.AspNetCore.Options;
using Civitas.Id.Sweden.Format;
using Civitas.Id.Sweden.Json.Extensions;
using Civitas.Id.Sweden.Json.Options;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Civitas.Id.Sweden.AspNetCore.Extensions;

// ReSharper disable ConvertToExtensionBlock
// Rationale: C# 14 extension(T) syntax triggers CA1034 false positive
// (dotnet/sdk#51681 fix is .NET 11 SDK only). Microsoft has not migrated
// its own DI extension methods to the new syntax. IL is identical.

/// <summary>
/// Extension methods for registering the Civitas.Id.Sweden ASP.NET Core integration.
/// </summary>
[PublicAPI]
public static class CivitasIdSwedenAspNetCoreServiceCollectionExtensions
{
    /// <summary>
    /// Adds Civitas.Id.Sweden ASP.NET Core integration with default options.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <returns>The same service collection for chaining.</returns>
    /// <remarks>
    /// Registers: <c>ProblemDetails</c>, the
    /// <see cref="InvalidIdNumberExceptionHandler"/>, the JSON package
    /// converters bridged onto the ASP.NET Core JSON pipeline, and validation
    /// for <see cref="CivitasIdSwedenAspNetCoreOptions"/>.
    /// <para>
    /// OpenAPI schema metadata is OPT-IN; call
    /// <c>builder.Services.AddOpenApi(opts =&gt; opts.AddCivitasIdSwedenSchemas())</c>
    /// to enrich the emitted document.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddCivitasIdSwedenAspNetCore(this IServiceCollection services)
        => services.AddCivitasIdSwedenAspNetCore(static _ => { });

    /// <summary>
    /// Adds Civitas.Id.Sweden ASP.NET Core integration with custom options.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <param name="configure">A delegate that configures the options.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddCivitasIdSwedenAspNetCore(
        this IServiceCollection services,
        Action<CivitasIdSwedenAspNetCoreOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        RegisterInfrastructure(services);

        services.AddOptions<CivitasIdSwedenAspNetCoreOptions>()
            .Configure(configure)
            .Validate(static o => o.JsonFormat is PnrFormat.LongFormat or PnrFormat.ShortFormat,
                "JsonFormat must be LongFormat or ShortFormat.")
            .ValidateOnStart();

        return services;
    }

    /// <summary>
    /// Adds Civitas.Id.Sweden ASP.NET Core integration, binding options from the
    /// supplied configuration <paramref name="section"/>.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <param name="section">The configuration section to bind from.</param>
    /// <returns>The same service collection for chaining.</returns>
    /// <remarks>
    /// For AOT-safe publishing, the consumer csproj must set
    /// <c>&lt;EnableConfigurationBindingGenerator&gt;true&lt;/EnableConfigurationBindingGenerator&gt;</c>.
    /// <para>
    /// Note: <see cref="CivitasIdSwedenAspNetCoreOptions.RedactInput"/> is a delegate
    /// and is therefore NOT bound from JSON config; it stays <see langword="null"/>
    /// on this overload. Use the <see cref="Action{T}"/> overload to set it.
    /// </para>
    /// </remarks>
    [RequiresUnreferencedCode("Binds CivitasIdSwedenAspNetCoreOptions from configuration. Enable the configuration binding source generator (<EnableConfigurationBindingGenerator>true</EnableConfigurationBindingGenerator>) for AOT/trim-safe builds.")]
    [RequiresDynamicCode("Binds CivitasIdSwedenAspNetCoreOptions from configuration. Enable the configuration binding source generator (<EnableConfigurationBindingGenerator>true</EnableConfigurationBindingGenerator>) for AOT/trim-safe builds.")]
    public static IServiceCollection AddCivitasIdSwedenAspNetCore(
        this IServiceCollection services,
        IConfiguration section)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(section);

        RegisterInfrastructure(services);

        services.AddOptions<CivitasIdSwedenAspNetCoreOptions>()
            .Bind(section)
            .Validate(static o => o.JsonFormat is PnrFormat.LongFormat or PnrFormat.ShortFormat,
                "JsonFormat must be LongFormat or ShortFormat.")
            .ValidateOnStart();

        return services;
    }

    private static void RegisterInfrastructure(IServiceCollection services)
    {
        services.AddProblemDetails();

        // Use TryAddEnumerable for true idempotency on IExceptionHandler
        // (services.AddExceptionHandler<T> uses AddSingleton which would duplicate).
        services.TryAddEnumerable(ServiceDescriptor.Singleton<
            IExceptionHandler,
            InvalidIdNumberExceptionHandler>());

        services.TryAddEnumerable(ServiceDescriptor.Singleton<
            IValidateOptions<CivitasIdSwedenAspNetCoreOptions>,
            ValidateCivitasIdSwedenAspNetCoreOptions>());

        // NOTE: NO services.AddOpenApi(...) — opt-in via
        // OpenApiOptions.AddCivitasIdSwedenSchemas(). AddOpenApi() is not
        // idempotent across document names and calling it from library code
        // can silently conflict with the consumer's named documents
        // (OpenTelemetry rationale: library authors register transformers,
        // never call AddOpenApi() / AddOpenTelemetry()).

        services.AddCivitasIdSwedenJson(static _ => { });
        services.AddOptions<CivitasIdSwedenJsonOptions>()
            .Configure<IOptions<CivitasIdSwedenAspNetCoreOptions>>(static (jsonOpts, aspOpts) =>
                jsonOpts.PersonnummerFormat = aspOpts.Value.JsonFormat);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<
            IConfigureOptions<JsonOptions>,
            CivitasIdHttpJsonOptionsSetup>());
    }
}
