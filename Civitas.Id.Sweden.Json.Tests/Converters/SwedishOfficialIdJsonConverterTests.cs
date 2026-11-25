using System.Text.Json;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Json.Converters;

namespace Civitas.Id.Sweden.Json.Tests.Converters;

public class SwedishOfficialIdJsonConverterTests
{
    private static readonly JsonSerializerOptions Opts = new()
    {
        Converters = { new SwedishOfficialIdLongFormatJsonConverter() }
    };

    public class Deserialize
    {
        public static IEnumerable<(string Input, Type ExpectedType)> MaterializesCorrectSubtypeInputs()
        {
            yield return (OfficialNumbers.Centenarian.Long, typeof(PersonalId));
            yield return (OfficialNumbers.CentenarianSamordningsnummer.Long, typeof(CoordinationId));
            yield return (LegalPersonNumbers.Aktiebolag, typeof(OrganisationId));
        }

        [Test]
        [MethodDataSource(nameof(MaterializesCorrectSubtypeInputs))]
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
            SwedishOfficialId id = PersonalId.Parse(OfficialNumbers.Centenarian.Long);
            var json = JsonSerializer.Serialize(id, Opts);
            await Assert.That(json).IsEqualTo($"\"{OfficialNumbers.Centenarian.Long}\"");
        }

        [Test]
        public async Task WritesLongFormat_ForCoordinationId()
        {
            SwedishOfficialId id = CoordinationId.Parse(OfficialNumbers.CentenarianSamordningsnummer.Long);
            var json = JsonSerializer.Serialize(id, Opts);
            await Assert.That(json).IsEqualTo($"\"{OfficialNumbers.CentenarianSamordningsnummer.Long}\"");
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
