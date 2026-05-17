using System.Diagnostics.CodeAnalysis;
using Civitas.Id.Sweden.AspNetCore.ExceptionHandlers;
using Civitas.Id.Sweden.AspNetCore.Json;
using Civitas.Id.Sweden.AspNetCore.OpenApi;
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

/// <summary>
/// Extension methods for registering the Civitas.Id.Sweden ASP.NET Core integration.
/// </summary>
[PublicAPI]
[SuppressMessage(
    "Design", "CA1034:Nested types should not be visible",
    Justification = "C# 14 'extension(T)' block is compiled to a nested type by the language; not authored by us.")]
public static class CivitasIdSwedenAspNetCoreServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds Civitas.Id.Sweden ASP.NET Core integration with default options.
        /// </summary>
        /// <returns>The same service collection for chaining.</returns>
        /// <remarks>
        /// Registers: <c>ProblemDetails</c>, the <see cref="InvalidIdNumberExceptionHandler"/>,
        /// the <see cref="SwedishIdSchemaTransformer"/> on the default OpenAPI document,
        /// the JSON package converters bridged onto the ASP.NET Core JSON pipeline, and
        /// validation for <see cref="CivitasIdSwedenAspNetCoreOptions"/>.
        /// </remarks>
        public IServiceCollection AddCivitasIdSwedenAspNetCore()
            => services.AddCivitasIdSwedenAspNetCore(static _ => { });

        /// <summary>
        /// Adds Civitas.Id.Sweden ASP.NET Core integration with custom options.
        /// </summary>
        /// <param name="configure">A delegate that configures the options.</param>
        /// <returns>The same service collection for chaining.</returns>
        public IServiceCollection AddCivitasIdSwedenAspNetCore(Action<CivitasIdSwedenAspNetCoreOptions> configure)
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
        public IServiceCollection AddCivitasIdSwedenAspNetCore(IConfiguration section)
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

        services.AddOpenApi(opts => opts.AddSchemaTransformer<SwedishIdSchemaTransformer>());

        services.AddCivitasIdSwedenJson(static _ => { });
        services.AddOptions<CivitasIdSwedenJsonOptions>()
            .Configure<IOptions<CivitasIdSwedenAspNetCoreOptions>>(static (jsonOpts, aspOpts) =>
                jsonOpts.PersonnummerFormat = aspOpts.Value.JsonFormat);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<
            IConfigureOptions<JsonOptions>,
            CivitasIdHttpJsonOptionsSetup>());
    }
}
