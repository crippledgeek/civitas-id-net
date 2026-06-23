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
            var id = CoordinationId.Parse("191401682396");
            var json = JsonSerializer.Serialize(id, Opts);
            await Assert.That(json).IsEqualTo("\"1401682396\"");
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
            // "1401682396" short-form: century inferred by sliding window from today.
            // Year "14" is within the last 100 years from 2026, so resolves to 2014.
            var id = JsonSerializer.Deserialize<CoordinationId>("\"1401682396\"", Opts);
            await Assert.That(id).IsNotNull();
            await Assert.That(id!.LongFormat()).IsEqualTo("201401682396");
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
        [Test]
        [Arguments("200805680006")]
        public async Task RoundTrips(string input)
        {
            // Round-trip via ShortFormat is only stable for IDs whose 2-digit year
            // is unambiguous under the sliding 100-year window (within the last ~100 years).
            // "200805680006" = born 2008-05-08 (day +60 = 68), Luhn-valid.
            var original = CoordinationId.Parse(input);
            var json = JsonSerializer.Serialize(original, Opts);
            var roundTripped = JsonSerializer.Deserialize<CoordinationId>(json, Opts);
            await Assert.That(roundTripped).IsEqualTo(original);
        }
    }
}
