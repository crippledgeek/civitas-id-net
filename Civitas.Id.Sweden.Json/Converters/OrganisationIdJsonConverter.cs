namespace Civitas.Id.Sweden.Json.Converters;

using System.Text.Json;
using System.Text.Json.Serialization;
using Civitas.Id.Sweden.Core;
using JetBrains.Annotations;

/// <summary>
/// Serializes and deserializes <see cref="OrganisationId"/> values as 10-digit
/// canonical strings (LongFormat).
/// </summary>
[PublicAPI]
public sealed class OrganisationIdJsonConverter : JsonConverter<OrganisationId>
{
    /// <inheritdoc />
    public override bool HandleNull => true;

    /// <inheritdoc />
    public override OrganisationId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            throw new JsonException("OrganisationId is null");
        }

        return OrganisationId.Parse(reader.GetString() ?? throw new JsonException("OrganisationId is null"));
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, OrganisationId value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);
        writer.WriteStringValue(value.LongFormat());
    }
}
