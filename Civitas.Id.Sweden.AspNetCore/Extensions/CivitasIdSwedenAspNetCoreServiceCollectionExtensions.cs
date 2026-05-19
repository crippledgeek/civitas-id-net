using System.Diagnostics.CodeAnalysis;
using Civitas.Id.Sweden.AspNetCore.Constants;
using Civitas.Id.Sweden.AspNetCore.ExceptionHandlers;
using Civitas.Id.Sweden.AspNetCore.Json;
using Civitas.Id.Sweden.AspNetCore.Options;
using Civitas.Id.Sweden.Format;
using Civitas.Id.Sweden.Json.Extensions;
using Civitas.Id.Sweden.Json.Options;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
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
    /// The framework's RFC 9110 default ProblemDetails Type URI emitted when no
    /// explicit Type is set on a 400 response. Identified by string comparison
    /// so the library can replace it without stomping consumer-set Type values
    /// from non-library domains.
    /// </summary>
    private const string FrameworkRfc9110BadRequestType = "https://tools.ietf.org/html/rfc9110#section-15.5.1";

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
    /// <remarks>
    /// OpenAPI schema metadata is opt-in. Call
    /// <c>builder.Services.AddOpenApi(opts =&gt; opts.AddCivitasIdSwedenSchemas())</c>
    /// to register the schema transformer that enriches the emitted document.
    /// </remarks>
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
            .Validate(static o =>
                    Uri.TryCreate(o.ProblemDetailsTypeBaseUri, UriKind.Absolute, out var uri)
                    && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
                    && string.IsNullOrEmpty(uri.UserInfo)
                    && o.ProblemDetailsTypeBaseUri.EndsWith('/'),
                "ProblemDetailsTypeBaseUri must be an absolute https or http URI ending with '/' and must not contain user info.")
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
    /// <para>
    /// OpenAPI schema metadata is opt-in. Call
    /// <c>builder.Services.AddOpenApi(opts =&gt; opts.AddCivitasIdSwedenSchemas())</c>
    /// to register the schema transformer that enriches the emitted document.
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
            .Validate(static o =>
                    Uri.TryCreate(o.ProblemDetailsTypeBaseUri, UriKind.Absolute, out var uri)
                    && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
                    && string.IsNullOrEmpty(uri.UserInfo)
                    && o.ProblemDetailsTypeBaseUri.EndsWith('/'),
                "ProblemDetailsTypeBaseUri must be an absolute https or http URI ending with '/' and must not contain user info.")
            .ValidateOnStart();

        return services;
    }

    private static void RegisterInfrastructure(IServiceCollection services)
    {
        // CustomizeProblemDetails normalizes the `type` URI across BOTH surfaces:
        //   1. Minimal API ProblemDetails (IProblemDetailsService.TryWriteAsync)
        //   2. MVC ValidationProblemDetails (DefaultProblemDetailsFactory)
        // Verified against aspnetcore release/10.0 source: the factory's
        // ApplyProblemDetailsDefaults invokes _configure (= CustomizeProblemDetails)
        // for both CreateProblemDetails and CreateValidationProblemDetails. The
        // closure-allocating lambda is acceptable here because we need
        // ctx.HttpContext.RequestServices to resolve the IOptions instance.
        services.AddProblemDetails(opts =>
        {
            opts.CustomizeProblemDetails = ctx =>
            {
                // Only normalize 400 ProblemDetails. Non-400 responses pass
                // through untouched.
                if (ctx.ProblemDetails.Status != StatusCodes.Status400BadRequest)
                {
                    return;
                }

                var aspOpts = ctx.HttpContext.RequestServices
                    .GetRequiredService<IOptions<CivitasIdSwedenAspNetCoreOptions>>();
                var baseUri = aspOpts.Value.ProblemDetailsTypeBaseUri;

                // Replace ONLY the framework's RFC 9110 default OR null/empty.
                // Consumer-set non-library Type values (e.g., from third-party
                // domains) are preserved. Library-set values (already under
                // baseUri) are left alone for idempotency.
                if (string.IsNullOrEmpty(ctx.ProblemDetails.Type)
                    || string.Equals(ctx.ProblemDetails.Type, FrameworkRfc9110BadRequestType, StringComparison.Ordinal))
                {
                    ctx.ProblemDetails.Type = $"{baseUri}{ProblemTypeFragments.BadRequest}";
                }
            };
        });

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

        // Gap A bridge — mirror onto Mvc.JsonOptions so SystemTextJsonInputFormatter
        // (MVC [FromBody]) sees the converters. Safe no-op when AddControllers()
        // is never called: Mvc.JsonOptions is never resolved → Configure never fires.
        services.TryAddEnumerable(ServiceDescriptor.Singleton<
            IConfigureOptions<Microsoft.AspNetCore.Mvc.JsonOptions>,
            CivitasIdMvcJsonOptionsSetup>());

        services.AddHostedService<Hosting.CivitasIdSwedenStartupValidator>();
    }
}
