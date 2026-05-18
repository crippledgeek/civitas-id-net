using System.Text.Json;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Json.Converters;

namespace Civitas.Id.Sweden.Json.Tests.Converters;

public class CoordinationIdJsonConverterTests
{
    private static readonly JsonSerializerOptions Opts = new()
    {
        Converters = { new CoordinationIdLongFormatJsonConverter() }
    };

    public class Serialize
    {
        [Test]
        public async Task WritesLongFormatString()
        {
            var id = CoordinationId.Parse("191401682396");
            var json = JsonSerializer.Serialize(id, Opts);
            await Assert.That(json).IsEqualTo("\"191401682396\"");
        }
    }

    public class Deserialize
    {
        [Test]
        public async Task ParsesLongFormatString()
        {
            var id = JsonSerializer.Deserialize<CoordinationId>("\"191401682396\"", Opts);
            await Assert.That(id).IsNotNull();
            await Assert.That(id!.LongFormat()).IsEqualTo("191401682396");
        }

        [Test]
        public async Task ParsesShortFormatString()
        {
            var id = JsonSerializer.Deserialize<CoordinationId>("\"140168-2396\"", Opts);
            await Assert.That(id).IsNotNull();
        }

        [Test]
        public async Task ThrowsJsonException_OnNull()
        {
            await Assert.That(() => JsonSerializer.Deserialize<CoordinationId>("null", Opts))
                .Throws<JsonException>();
        }

        [Test]
        public async Task ThrowsException_OnMalformed()
        {
            await Assert.That(() => JsonSerializer.Deserialize<CoordinationId>("\"not-a-pnr\"", Opts))
                .ThrowsException();
        }
    }

    public class RoundTrip
    {
        [Test]
        [Arguments("191401682396")]
        [Arguments("19140168-2396")]
        public async Task RoundTrips(string input)
        {
            var original = CoordinationId.Parse(input);
            var json = JsonSerializer.Serialize(original, Opts);
            var roundTripped = JsonSerializer.Deserialize<CoordinationId>(json, Opts);
            await Assert.That(roundTripped).IsEqualTo(original);
        }
    }
}
