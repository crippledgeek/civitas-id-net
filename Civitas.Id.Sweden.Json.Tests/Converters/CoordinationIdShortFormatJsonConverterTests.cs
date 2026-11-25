using System.Text.Json;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Json.Converters;

namespace Civitas.Id.Sweden.Json.Tests.Converters;

public class CoordinationIdShortFormatJsonConverterTests
{
    private static readonly JsonSerializerOptions Opts = new()
    {
        Converters = { new CoordinationIdShortFormatJsonConverter() }
    };

    public class Serialize
    {
        [Test]
        public async Task WritesShortFormatString_TenDigitsNoSeparator()
        {
            // CoordinationId day component is +60; YYYYMMDDXXXX where DD is 61–91.
            // ShortFormat = YYMMDDXXXX (10 digits, no separator).
            var id = CoordinationId.Parse(OfficialNumbers.CentenarianSamordningsnummer.Long);
            var json = JsonSerializer.Serialize(id, Opts);
            await UnprintedAssert.AreEqual(json, $"\"{OfficialNumbers.CentenarianSamordningsnummer.Short}\"")
                .ConfigureAwait(false);
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
            // Short form: the century is inferred by a sliding window from today, so year "81" resolves to 1981.
            var id = JsonSerializer.Deserialize<CoordinationId>($"\"{OfficialNumbers.TwentiethCenturySamordningsnummer.Short}\"", Opts);
            await Assert.That(id).IsNotNull();
            await Assert.That(id!.LongFormat()).IsEqualTo(OfficialNumbers.TwentiethCenturySamordningsnummer.Long);
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
            await Assert.That(() => JsonSerializer.Deserialize<CoordinationId>("\"not-a-coordination-id\"", Opts))
                .ThrowsException();
        }
    }

    public class RoundTrip
    {
        public static IEnumerable<string> RoundTripsInputs()
        {
            yield return OfficialNumbers.ModernSamordningsnummer.Long;
            yield return OfficialNumbers.TwentiethCenturySamordningsnummer.Long;
        }

        [Test]
        [MethodDataSource(nameof(RoundTripsInputs))]
        public async Task RoundTrips(string input)
        {
            // Round-trip via ShortFormat is only stable for IDs whose 2-digit year
            // is unambiguous under the sliding 100-year window (within the last ~100 years).
            var original = CoordinationId.Parse(input);
            var json = JsonSerializer.Serialize(original, Opts);
            var roundTripped = JsonSerializer.Deserialize<CoordinationId>(json, Opts);
            await Assert.That(roundTripped).IsEqualTo(original);
        }
    }
}
