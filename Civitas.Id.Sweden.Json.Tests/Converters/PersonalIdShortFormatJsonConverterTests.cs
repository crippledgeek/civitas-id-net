using System.Text.Json;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Json.Converters;

namespace Civitas.Id.Sweden.Json.Tests.Converters;

public class PersonalIdShortFormatJsonConverterTests
{
    private static readonly JsonSerializerOptions Opts = new()
    {
        Converters = { new PersonalIdShortFormatJsonConverter() }
    };

    public class Serialize
    {
        [Test]
        public async Task WritesShortFormatString_TenDigitsNoSeparator()
        {
            // ShortFormat = YYMMDDXXXX (10 digits, no separator), never a "+" centenarian sentinel.
            var id = PersonalId.Parse("198112189876");
            var json = JsonSerializer.Serialize(id, Opts);
            await Assert.That(json).IsEqualTo("\"8112189876\"");
        }

        [Test]
        public async Task WritesShortFormat_For1890s_TenDigitsNoSeparator()
        {
            // Even for centenarians the converter emits 10-digit no-separator form
            // (PnrFormat.ShortFormat strips chars[2..] of the canonical 12-digit form).
            var id = PersonalId.Parse("189001019802");
            var json = JsonSerializer.Serialize(id, Opts);
            await Assert.That(json).IsEqualTo("\"9001019802\"");
        }
    }

    public class Deserialize
    {
        [Test]
        public async Task ParsesLongFormatString()
        {
            var id = JsonSerializer.Deserialize<PersonalId>("\"198112189876\"", Opts);
            await Assert.That(id).IsNotNull();
            await Assert.That(id!.LongFormat()).IsEqualTo("198112189876");
        }

        [Test]
        public async Task ParsesShortFormatString()
        {
            var id = JsonSerializer.Deserialize<PersonalId>("\"8112189876\"", Opts);
            await Assert.That(id).IsNotNull();
        }

        [Test]
        public async Task ThrowsJsonException_OnNull()
        {
            await Assert.That(() => JsonSerializer.Deserialize<PersonalId>("null", Opts))
                .Throws<JsonException>();
        }

        [Test]
        public async Task ThrowsException_OnMalformed()
        {
            await Assert.That(() => JsonSerializer.Deserialize<PersonalId>("\"not-a-pnr\"", Opts))
                .ThrowsException();
        }
    }

    public class RoundTrip
    {
        [Test]
        [Arguments("198112189876")]
        [Arguments("200805080009")]
        public async Task RoundTrips(string input)
        {
            // Round-trip via ShortFormat is only stable for IDs whose 2-digit year
            // is unambiguous under the sliding 100-year window.
            // Centenarian IDs (born before ~1926) require the "+" separator
            // (PnrFormat.ShortFormatWithSeparator) to round-trip without loss.
            var original = PersonalId.Parse(input);
            var json = JsonSerializer.Serialize(original, Opts);
            var roundTripped = JsonSerializer.Deserialize<PersonalId>(json, Opts);
            await Assert.That(roundTripped).IsEqualTo(original);
        }
    }
}
