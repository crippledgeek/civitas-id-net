using System.Text.Json;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Json.Converters;

namespace Civitas.Id.Sweden.Json.Tests.Converters;

public class SwedishOfficialIdShortFormatJsonConverterTests
{
    private static readonly JsonSerializerOptions Opts = new()
    {
        Converters = { new SwedishOfficialIdShortFormatJsonConverter() }
    };

    public class Deserialize
    {
        [Test]
        [Arguments("198112189876", typeof(PersonalId))]
        [Arguments("191401682396", typeof(CoordinationId))]
        [Arguments("5560160680", typeof(OrganisationId))]
        public async Task MaterializesCorrectSubtype(string input, Type expectedType)
        {
            var id = JsonSerializer.Deserialize<SwedishOfficialId>($"\"{input}\"", Opts);
            await Assert.That(id?.GetType()).IsEqualTo(expectedType);
        }

        [Test]
        public async Task ThrowsJsonException_OnNull()
        {
            await Assert.That(() => JsonSerializer.Deserialize<SwedishOfficialId>("null", Opts))
                .Throws<JsonException>();
        }

        [Test]
        public async Task ThrowsException_OnMalformed()
        {
            await Assert.That(() => JsonSerializer.Deserialize<SwedishOfficialId>("\"not-an-id\"", Opts))
                .ThrowsException();
        }
    }

    public class Serialize
    {
        [Test]
        public async Task WritesShortFormat_ForPersonalId()
        {
            // PersonalId is written using PnrFormat.ShortFormat = 10-digit no-separator.
            SwedishOfficialId id = PersonalId.Parse("198112189876");
            var json = JsonSerializer.Serialize(id, Opts);
            await Assert.That(json).IsEqualTo("\"8112189876\"");
        }

        [Test]
        public async Task WritesShortFormat_ForCoordinationId()
        {
            // CoordinationId is also written as 10-digit no-separator short form.
            SwedishOfficialId id = CoordinationId.Parse("191401682396");
            var json = JsonSerializer.Serialize(id, Opts);
            await Assert.That(json).IsEqualTo("\"1401682396\"");
        }

        [Test]
        public async Task WritesLongFormat_ForOrganisationId()
        {
            // OrganisationId has no ShortFormat axis; always written as 10-digit LongFormat.
            SwedishOfficialId id = OrganisationId.Parse("5560160680");
            var json = JsonSerializer.Serialize(id, Opts);
            await Assert.That(json).IsEqualTo("\"5560160680\"");
        }
    }

    public class RoundTrip
    {
        [Test]
        [Arguments("198112189876")]  // PersonalId — modern, round-trips via 10-digit ShortFormat
        [Arguments("200805680006")]  // CoordinationId (born 2008-05-08) — modern, round-trips
        [Arguments("5560160680")]    // OrganisationId — always 10-digit LongFormat, always round-trips
        public async Task RoundTrips(string input)
        {
            var original = SwedishOfficialId.ParseAny(input);
            var json = JsonSerializer.Serialize(original, Opts);
            var roundTripped = JsonSerializer.Deserialize<SwedishOfficialId>(json, Opts);
            await Assert.That(roundTripped?.GetType()).IsEqualTo(original.GetType());
        }
    }
}
