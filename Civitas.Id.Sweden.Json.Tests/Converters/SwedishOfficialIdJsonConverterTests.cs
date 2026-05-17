using System.Text.Json;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Json.Converters;

namespace Civitas.Id.Sweden.Json.Tests.Converters;

public class SwedishOfficialIdJsonConverterTests
{
    private static readonly JsonSerializerOptions Opts = new()
    {
        Converters = { new SwedishOfficialIdJsonConverter() }
    };

    public class Deserialize
    {
        [Test]
        [Arguments("189001019802", typeof(PersonalId))]
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
        public async Task WritesLongFormat_ForPersonalId()
        {
            SwedishOfficialId id = PersonalId.Parse("189001019802");
            var json = JsonSerializer.Serialize(id, Opts);
            await Assert.That(json).IsEqualTo("\"189001019802\"");
        }

        [Test]
        public async Task WritesLongFormat_ForCoordinationId()
        {
            SwedishOfficialId id = CoordinationId.Parse("191401682396");
            var json = JsonSerializer.Serialize(id, Opts);
            await Assert.That(json).IsEqualTo("\"191401682396\"");
        }

        [Test]
        public async Task WritesLongFormat_ForOrganisationId()
        {
            SwedishOfficialId id = OrganisationId.Parse("5560160680");
            var json = JsonSerializer.Serialize(id, Opts);
            await Assert.That(json).IsEqualTo("\"5560160680\"");
        }
    }
}
