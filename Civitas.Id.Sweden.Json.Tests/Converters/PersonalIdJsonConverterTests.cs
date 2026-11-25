using System.Text.Json;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Errors;
using Civitas.Id.Sweden.Json.Converters;

namespace Civitas.Id.Sweden.Json.Tests.Converters;

public class PersonalIdJsonConverterTests
{
    private static readonly JsonSerializerOptions Opts = new()
    {
        Converters = { new PersonalIdLongFormatJsonConverter() }
    };

    public class Serialize
    {
        [Test]
        public async Task WritesLongFormatString()
        {
            var id = PersonalId.Parse(OfficialNumbers.Centenarian.Long);
            var json = JsonSerializer.Serialize(id, Opts);
            await Assert.That(json).IsEqualTo($"\"{OfficialNumbers.Centenarian.Long}\"");
        }
    }

    public class Deserialize
    {
        [Test]
        public async Task ParsesLongFormatString()
        {
            var id = JsonSerializer.Deserialize<PersonalId>($"\"{OfficialNumbers.Centenarian.Long}\"", Opts);
            await Assert.That(id).IsNotNull();
            await Assert.That(id!.LongFormat()).IsEqualTo(OfficialNumbers.Centenarian.Long);
        }

        [Test]
        public async Task ParsesShortFormatString()
        {
            var id = JsonSerializer.Deserialize<PersonalId>($"\"{OfficialNumbers.Centenarian.ShortWithHyphen}\"", Opts);
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

    public class Generated
    {
        [Test]
        public async Task Valid_Parses_AndEveryDefectThrows()
        {
            var valid = JsonSerializer.Serialize(GeneratedNumbers.ValidPersonnummer);
            await Assert.That(JsonSerializer.Deserialize<PersonalId>(valid, Opts) is not null).IsTrue();

            foreach (var json in GeneratedNumbers.InvalidPersonnummer().Select(text => JsonSerializer.Serialize(text)))
                await Assert.That(() => { _ = JsonSerializer.Deserialize<PersonalId>(json, Opts); })
                    .Throws<InvalidIdNumberException>();
        }
    }

    public class RoundTrip
    {
        public static IEnumerable<string> RoundTripsInputs()
        {
            yield return OfficialNumbers.Centenarian.Long;
            yield return OfficialNumbers.Centenarian.LongWithHyphen;
        }

        [Test]
        [MethodDataSource(nameof(RoundTripsInputs))]
        public async Task RoundTrips(string input)
        {
            var original = PersonalId.Parse(input);
            var json = JsonSerializer.Serialize(original, Opts);
            var roundTripped = JsonSerializer.Deserialize<PersonalId>(json, Opts);
            await Assert.That(roundTripped).IsEqualTo(original);
        }
    }
}
