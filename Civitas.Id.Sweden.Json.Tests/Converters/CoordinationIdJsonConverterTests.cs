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
            var id = CoordinationId.Parse(OfficialNumbers.CentenarianSamordningsnummer.Long);
            var json = JsonSerializer.Serialize(id, Opts);
            await Assert.That(json).IsEqualTo($"\"{OfficialNumbers.CentenarianSamordningsnummer.Long}\"");
        }
    }

    public class Deserialize
    {
        [Test]
        public async Task ParsesLongFormatString()
        {
            var id = JsonSerializer.Deserialize<CoordinationId>($"\"{OfficialNumbers.CentenarianSamordningsnummer.Long}\"", Opts);
            await Assert.That(id).IsNotNull();
            await Assert.That(id!.LongFormat()).IsEqualTo(OfficialNumbers.CentenarianSamordningsnummer.Long);
        }

        [Test]
        public async Task ParsesShortFormatString()
        {
            var id = JsonSerializer.Deserialize<CoordinationId>($"\"{OfficialNumbers.CentenarianSamordningsnummer.ShortWithHyphen}\"", Opts);
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
        public static IEnumerable<string> RoundTripsInputs()
        {
            yield return OfficialNumbers.CentenarianSamordningsnummer.Long;
            yield return OfficialNumbers.CentenarianSamordningsnummer.LongWithHyphen;
        }

        [Test]
        [MethodDataSource(nameof(RoundTripsInputs))]
        public async Task RoundTrips(string input)
        {
            var original = CoordinationId.Parse(input);
            var json = JsonSerializer.Serialize(original, Opts);
            var roundTripped = JsonSerializer.Deserialize<CoordinationId>(json, Opts);
            await Assert.That(roundTripped).IsEqualTo(original);
        }
    }
}
