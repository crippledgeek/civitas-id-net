using System.Text.Json;
using System.Text.Json.Serialization;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Format;
using JetBrains.Annotations;

namespace Civitas.Id.Sweden.Json.Converters;

/// <summary>
/// Serializes <see cref="CoordinationId"/> values using the
/// <see cref="PnrFormat.ShortFormat"/> 10-digit wire form (with hyphen, or
/// "+" separator for centenarians) and deserializes any canonical Swedish
/// CoordinationId string. The centenarian "+" path requires reading Stockholm
/// civil time internally to determine whether the bearer is &gt;=100 years old.
/// </summary>
[PublicAPI]
public sealed class CoordinationIdShortFormatJsonConverter : JsonConverter<CoordinationId>
{
    /// <inheritdoc />
    public override bool HandleNull => true;

    /// <inheritdoc />
    public override CoordinationId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType == JsonTokenType.Null
            ? throw new JsonException("CoordinationId is null")
            : CoordinationId.Parse(reader.GetString() ?? throw new JsonException("CoordinationId is null"));
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, CoordinationId value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);
        writer.WriteStringValue(value.Format(PnrFormat.ShortFormat));
    }
}
