using System.Text.Json;
using Civitas.Id.Sweden.Format;
using Civitas.Id.Sweden.Json;
using Civitas.Id.Sweden.Json.Converters;

namespace Civitas.Id.Sweden.AspNetCore.Json;

/// <summary>
/// Installs the Civitas.Id.Sweden source-gen context and per-type converters
/// onto a <see cref="JsonSerializerOptions"/> instance. Shared between
/// <see cref="CivitasIdHttpJsonOptionsSetup"/> (minimal API) and a future
/// MVC-side setup. The converter set is selected based on the requested
/// <see cref="PnrFormat"/>.
/// </summary>
internal static class ConverterInstaller
{
    /// <summary>
    /// Chains <see cref="CivitasIdSwedenJsonContext.Default"/> onto the
    /// type-info resolver chain and registers the per-type converters
    /// that match <paramref name="format"/>.
    /// </summary>
    public static void Install(JsonSerializerOptions serializer, PnrFormat format)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        serializer.TypeInfoResolverChain.Add(CivitasIdSwedenJsonContext.Default);
        serializer.TypeInfoResolverChain.Add(CivitasIdAspNetCoreJsonContext.Default);

        if (format == PnrFormat.ShortFormat)
        {
            serializer.Converters.Add(new PersonalIdShortFormatJsonConverter());
            serializer.Converters.Add(new CoordinationIdShortFormatJsonConverter());
            serializer.Converters.Add(new OrganisationIdJsonConverter());
            serializer.Converters.Add(new SwedishOfficialIdShortFormatJsonConverter());
        }
        else
        {
            serializer.Converters.Add(new PersonalIdLongFormatJsonConverter());
            serializer.Converters.Add(new CoordinationIdLongFormatJsonConverter());
            serializer.Converters.Add(new OrganisationIdJsonConverter());
            serializer.Converters.Add(new SwedishOfficialIdLongFormatJsonConverter());
        }
    }
}
