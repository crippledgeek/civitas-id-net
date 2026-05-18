using System.Text.Json;
using System.Text.Json.Serialization;
using Civitas.Id.Sweden.Core;
using JetBrains.Annotations;

namespace Civitas.Id.Sweden.Json.Converters;

/// <summary>
/// Polymorphic converter that materializes <see cref="SwedishOfficialId"/>
/// values from canonical strings, dispatching to the correct concrete subtype
/// (<see cref="PersonalId"/>, <see cref="CoordinationId"/>, or
/// <see cref="OrganisationId"/>) via
/// <see cref="SwedishOfficialId.TryParseAny(string?, out SwedishOfficialId?)"/>.
/// </summary>
[PublicAPI]
public sealed class SwedishOfficialIdLongFormatJsonConverter : JsonConverter<SwedishOfficialId>
{
    /// <inheritdoc />
    public override bool HandleNull => true;

    /// <inheritdoc />
    public override SwedishOfficialId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType == JsonTokenType.Null
            ? throw new JsonException("SwedishOfficialId is null")
            : SwedishOfficialId.ParseAny(reader.GetString() ?? throw new JsonException("SwedishOfficialId is null"));
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, SwedishOfficialId value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);
        writer.WriteStringValue(value.LongFormat());
    }
}
