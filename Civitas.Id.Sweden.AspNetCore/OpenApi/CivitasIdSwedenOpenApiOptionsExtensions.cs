using JetBrains.Annotations;
using Microsoft.AspNetCore.OpenApi;

namespace Civitas.Id.Sweden.AspNetCore.OpenApi;

/// <summary>
/// OpenAPI configuration extensions for Civitas.Id.Sweden schema metadata.
/// </summary>
[PublicAPI]
public static class CivitasIdSwedenOpenApiOptionsExtensions
{
    /// <summary>
    /// Adds the <see cref="SwedishIdSchemaTransformer"/> to this OpenAPI
    /// document, enriching the schemas for <c>PersonalId</c>, <c>CoordinationId</c>,
    /// <c>OrganisationId</c>, and <c>SwedishOfficialId</c> with format, pattern,
    /// example, and description metadata.
    /// </summary>
    /// <param name="options">The OpenAPI options to extend.</param>
    /// <returns>The same <paramref name="options"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="options"/> is <see langword="null"/>.
    /// </exception>
    public static OpenApiOptions AddCivitasIdSwedenSchemas(this OpenApiOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.AddSchemaTransformer<SwedishIdSchemaTransformer>();
        return options;
    }
}
