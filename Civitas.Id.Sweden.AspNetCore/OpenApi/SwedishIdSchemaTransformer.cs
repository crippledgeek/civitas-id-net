using System.Text.Json.Nodes;
using Civitas.Id.Sweden.Core;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Civitas.Id.Sweden.AspNetCore.OpenApi;

/// <summary>
/// Augments OpenAPI schemas for Civitas.Id.Sweden types with string format,
/// regex pattern, example value, and human-readable description for
/// <see cref="PersonalId"/>, <see cref="CoordinationId"/>,
/// <see cref="OrganisationId"/>, and the polymorphic
/// <see cref="SwedishOfficialId"/> base type.
/// </summary>
internal sealed class SwedishIdSchemaTransformer : IOpenApiSchemaTransformer
{
    private const string PersonalIdPattern = @"^\d{6,8}[-+]?\d{4}$";
    private const string CoordinationIdPattern = @"^\d{6,8}[-+]?\d{4}$";
    private const string OrganisationIdPattern = @"^\d{10}$";
    private const string SwedishOfficialIdPattern = @"^\d{6,12}[-+]?\d{4}?$";

    /// <summary>
    /// Inspects the JSON type bound to the schema and, if it matches one of
    /// the Civitas.Id.Sweden ID types, sets the schema's string format,
    /// pattern, example, and description.
    /// </summary>
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(context);

        ApplyMetadata(schema, context.JsonTypeInfo.Type);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Applies the metadata to a schema. Exposed internally to support unit
    /// tests that exercise the per-type metadata without needing to construct
    /// an <see cref="OpenApiSchemaTransformerContext"/> (which has no public
    /// constructor in Microsoft.AspNetCore.OpenApi).
    /// </summary>
    internal static void ApplyMetadata(OpenApiSchema schema, Type type)
    {
        if (type == typeof(PersonalId))
        {
            Apply(schema, "personnummer", PersonalIdPattern,
                "198112189876", "Swedish personal ID (personnummer).");
        }
        else if (type == typeof(CoordinationId))
        {
            Apply(schema, "samordningsnummer", CoordinationIdPattern,
                "198112789876", "Swedish coordination number (samordningsnummer).");
        }
        else if (type == typeof(OrganisationId))
        {
            Apply(schema, "organisationsnummer", OrganisationIdPattern,
                "5560160680", "Swedish organisation number (organisationsnummer).");
        }
        else if (type == typeof(SwedishOfficialId))
        {
            Apply(schema, "swedish-official-id", SwedishOfficialIdPattern,
                "198112189876", "Swedish official ID — any of personnummer, samordningsnummer, or organisationsnummer.");
        }
    }

    private static void Apply(
        OpenApiSchema schema,
        string format,
        string pattern,
        string example,
        string description)
    {
        schema.Type = JsonSchemaType.String;
        schema.Format = format;
        schema.Pattern = pattern;
        schema.Example = JsonValue.Create(example);
        schema.Description = description;
    }
}
