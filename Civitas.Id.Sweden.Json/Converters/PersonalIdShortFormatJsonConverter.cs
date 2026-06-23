using System.Text.Json;
using System.Text.Json.Serialization;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Format;
using JetBrains.Annotations;

namespace Civitas.Id.Sweden.Json.Converters;

/// <summary>
/// Serializes <see cref="PersonalId"/> values using the
/// <see cref="PnrFormat.ShortFormat"/> 10-digit wire form (digits only, no
/// separator) and deserializes any canonical Swedish PersonalId string.
/// Deserializing a short (2-digit-year) form infers the century from Stockholm
/// civil time; the long (12-digit) form carries the century explicitly.
/// </summary>
[PublicAPI]
public sealed class PersonalIdShortFormatJsonConverter : JsonConverter<PersonalId>
{
    /// <inheritdoc />
    public override bool HandleNull => true;

    /// <inheritdoc />
    public override PersonalId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType == JsonTokenType.Null
            ? throw new JsonException("PersonalId is null")
            : PersonalId.Parse(reader.GetString() ?? throw new JsonException("PersonalId is null"));
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, PersonalId value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);
        writer.WriteStringValue(value.Format(PnrFormat.ShortFormat));
    }
}
