using Civitas.Id.Sweden.Json;
using Civitas.Id.Sweden.Json.Converters;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace Civitas.Id.Sweden.AspNetCore.Json;

/// <summary>
/// Bridges <see cref="CivitasIdSwedenJsonContext"/> into the ASP.NET Core
/// <see cref="JsonOptions"/> pipeline by chaining the source-generated
/// type info resolver onto the serializer options and registering the
/// per-type <see cref="System.Text.Json.Serialization.JsonConverter"/>
/// implementations that handle the canonical-string format.
/// </summary>
/// <remarks>
/// Lives in the <c>.AspNetCore</c> assembly (not <c>.Json</c>) because
/// <see cref="JsonOptions"/> requires
/// <c>FrameworkReference Microsoft.AspNetCore.App</c>.
/// </remarks>
internal sealed class CivitasIdHttpJsonOptionsSetup : IConfigureOptions<JsonOptions>
{
    /// <summary>
    /// Adds <see cref="CivitasIdSwedenJsonContext.Default"/> to the
    /// <see cref="JsonOptions.SerializerOptions"/> type-info resolver chain
    /// and registers the four canonical-string converters.
    /// </summary>
    public void Configure(JsonOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var serializer = options.SerializerOptions;
        serializer.TypeInfoResolverChain.Add(CivitasIdSwedenJsonContext.Default);

        // The source-generated context provides JsonTypeInfo metadata; the
        // converters handle the wire-format (canonical 12-digit strings).
        // Both pieces are required for round-trip deserialization of
        // PersonalId / CoordinationId / OrganisationId / SwedishOfficialId,
        // including when the type appears as a top-level body parameter or
        // nested inside a DTO.
        serializer.Converters.Add(new PersonalIdJsonConverter());
        serializer.Converters.Add(new CoordinationIdJsonConverter());
        serializer.Converters.Add(new OrganisationIdJsonConverter());
        serializer.Converters.Add(new SwedishOfficialIdJsonConverter());
    }
}
