using System.Diagnostics.CodeAnalysis;
using Civitas.Id.Sweden.Format;
using Civitas.Id.Sweden.Json.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Civitas.Id.Sweden.Json.Extensions;

/// <summary>
/// Extension methods for registering the Civitas.Id.Sweden System.Text.Json integration.
/// </summary>
[PublicAPI]
[SuppressMessage(
    "Design", "CA1034:Nested types should not be visible",
    Justification = "C# 14 'extension(T)' block is compiled to a nested type by the language; not authored by us.")]
public static class CivitasIdSwedenJsonServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Adds Civitas.Id.Sweden JSON integration with default options.</summary>
        /// <returns>The same service collection for chaining.</returns>
        public IServiceCollection AddCivitasIdSwedenJson()
            => services.AddCivitasIdSwedenJson(static _ => { });

        /// <summary>Adds Civitas.Id.Sweden JSON integration with custom options.</summary>
        /// <param name="configure">A delegate that configures the options.</param>
        /// <returns>The same service collection for chaining.</returns>
        public IServiceCollection AddCivitasIdSwedenJson(Action<CivitasIdSwedenJsonOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configure);

            services.TryAddEnumerable(ServiceDescriptor.Singleton<
                IValidateOptions<CivitasIdSwedenJsonOptions>,
                ValidateCivitasIdSwedenJsonOptions>());

            services.AddOptions<CivitasIdSwedenJsonOptions>()
                .Configure(configure)
                .Validate(static o => o.PersonnummerFormat is PnrFormat.LongFormat or PnrFormat.ShortFormat,
                    "PersonnummerFormat must be LongFormat or ShortFormat.")
                .ValidateOnStart();

            return services;
        }

        /// <summary>
        /// Adds Civitas.Id.Sweden JSON integration, binding options from an
        /// <see cref="IConfiguration"/> section.
        /// </summary>
        /// <param name="section">The configuration section to bind from.</param>
        /// <returns>The same service collection for chaining.</returns>
        /// <remarks>
        /// For AOT-safe publishing, the consumer csproj must set
        /// <c>&lt;EnableConfigurationBindingGenerator&gt;true&lt;/EnableConfigurationBindingGenerator&gt;</c>.
        /// </remarks>
        [RequiresUnreferencedCode("Binds CivitasIdSwedenJsonOptions from configuration. Enable the configuration binding source generator (<EnableConfigurationBindingGenerator>true</EnableConfigurationBindingGenerator>) for AOT/trim-safe builds.")]
        [RequiresDynamicCode("Binds CivitasIdSwedenJsonOptions from configuration. Enable the configuration binding source generator (<EnableConfigurationBindingGenerator>true</EnableConfigurationBindingGenerator>) for AOT/trim-safe builds.")]
        public IServiceCollection AddCivitasIdSwedenJson(IConfiguration section)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(section);

            services.TryAddEnumerable(ServiceDescriptor.Singleton<
                IValidateOptions<CivitasIdSwedenJsonOptions>,
                ValidateCivitasIdSwedenJsonOptions>());

            services.AddOptions<CivitasIdSwedenJsonOptions>()
                .Bind(section)
                .Validate(static o => o.PersonnummerFormat is PnrFormat.LongFormat or PnrFormat.ShortFormat,
                    "PersonnummerFormat must be LongFormat or ShortFormat.")
                .ValidateOnStart();

            return services;
        }
    }
}
