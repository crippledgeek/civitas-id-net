using Civitas.Id.Sweden.Json.Options;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace Civitas.Id.Sweden.AspNetCore.Json;

/// <summary>
/// Bridges <see cref="Civitas.Id.Sweden.Json.CivitasIdSwedenJsonContext"/> into
/// the ASP.NET Core <see cref="JsonOptions"/> pipeline by chaining the
/// source-generated type info resolver onto the serializer options and
/// registering the per-type <see cref="System.Text.Json.Serialization.JsonConverter"/>
/// implementations that match the configured
/// <see cref="CivitasIdSwedenJsonOptions.PersonnummerFormat"/>.
/// </summary>
/// <remarks>
/// Lives in the <c>.AspNetCore</c> assembly (not <c>.Json</c>) because
/// <see cref="JsonOptions"/> requires
/// <c>FrameworkReference Microsoft.AspNetCore.App</c>.
/// </remarks>
internal sealed class CivitasIdHttpJsonOptionsSetup(IOptions<CivitasIdSwedenJsonOptions> jsonOpts)
    : IConfigureOptions<JsonOptions>
{
    /// <summary>
    /// Adds <see cref="Civitas.Id.Sweden.Json.CivitasIdSwedenJsonContext.Default"/>
    /// to the <see cref="JsonOptions.SerializerOptions"/> type-info resolver
    /// chain and registers the format-aware canonical-string converters.
    /// </summary>
    public void Configure(JsonOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ConverterInstaller.Install(options.SerializerOptions, jsonOpts.Value.PersonnummerFormat);
    }
}
