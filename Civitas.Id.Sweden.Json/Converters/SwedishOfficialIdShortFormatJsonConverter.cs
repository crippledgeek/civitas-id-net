using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Format;
using JetBrains.Annotations;

namespace Civitas.Id.Sweden.Json.Converters;

/// <summary>
/// Polymorphic converter that materializes <see cref="SwedishOfficialId"/>
/// values from canonical strings and writes them using the
/// <see cref="PnrFormat.ShortFormat"/> wire form for
/// <see cref="PersonalId"/> / <see cref="CoordinationId"/>.
/// <see cref="OrganisationId"/> has no ShortFormat axis and is always
/// written as its 10-digit LongFormat.
/// </summary>
[PublicAPI]
public sealed class SwedishOfficialIdShortFormatJsonConverter : JsonConverter<SwedishOfficialId>
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
        writer.WriteStringValue(value switch
        {
            PersonalId p => p.Format(PnrFormat.ShortFormat),
            CoordinationId c => c.Format(PnrFormat.ShortFormat),
            OrganisationId o => o.LongFormat(),
            _ => throw new UnreachableException($"Unhandled SwedishOfficialId subtype: {value.GetType()}")
        });
    }
}
