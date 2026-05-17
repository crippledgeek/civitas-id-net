namespace Civitas.Id.Sweden.Json.Tests.Converters;

using System.Text.Json;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Json.Converters;

public class PersonalIdJsonConverterTests
{
    private static readonly JsonSerializerOptions Opts = new()
    {
        Converters = { new PersonalIdJsonConverter() },
    };

    public class Serialize
    {
        [Test]
        public async Task WritesLongFormatString()
        {
            var id = PersonalId.Parse("189001019802");
            var json = JsonSerializer.Serialize(id, Opts);
            await Assert.That(json).IsEqualTo("\"189001019802\"");
        }
    }

    public class Deserialize
    {
        [Test]
        public async Task ParsesLongFormatString()
        {
            var id = JsonSerializer.Deserialize<PersonalId>("\"189001019802\"", Opts);
            await Assert.That(id).IsNotNull();
            await Assert.That(id!.LongFormat()).IsEqualTo("189001019802");
        }

        [Test]
        public async Task ParsesShortFormatString()
        {
            var id = JsonSerializer.Deserialize<PersonalId>("\"900101-9802\"", Opts);
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
        [Arguments("189001019802")]
        [Arguments("18900101-9802")]
        public async Task RoundTrips(string input)
        {
            var original = PersonalId.Parse(input);
            var json = JsonSerializer.Serialize(original, Opts);
            var roundTripped = JsonSerializer.Deserialize<PersonalId>(json, Opts);
            await Assert.That(roundTripped).IsEqualTo(original);
        }
    }
}
