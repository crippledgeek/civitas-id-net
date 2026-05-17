namespace Civitas.Id.Sweden.Json.Converters;

using System.Text.Json;
using System.Text.Json.Serialization;
using Civitas.Id.Sweden.Core;
using JetBrains.Annotations;

/// <summary>
/// Serializes and deserializes <see cref="PersonalId"/> values as 12-digit
/// canonical strings (LongFormat).
/// </summary>
[PublicAPI]
public sealed class PersonalIdJsonConverter : JsonConverter<PersonalId>
{
    /// <inheritdoc />
    public override bool HandleNull => true;

    /// <inheritdoc />
    public override PersonalId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            throw new JsonException("PersonalId is null");
        }

        return PersonalId.Parse(reader.GetString() ?? throw new JsonException("PersonalId is null"));
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, PersonalId value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);
        writer.WriteStringValue(value.LongFormat());
    }
}
