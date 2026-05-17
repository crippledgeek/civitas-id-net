using System.Text.Json;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Json.Converters;

namespace Civitas.Id.Sweden.Json.Tests.Converters;

public class OrganisationIdJsonConverterTests
{
    private static readonly JsonSerializerOptions Opts = new()
    {
        Converters = { new OrganisationIdJsonConverter() }
    };

    public class Serialize
    {
        [Test]
        public async Task WritesLongFormatString_ForLegalPerson()
        {
            var id = OrganisationId.Parse("5560160680");
            var json = JsonSerializer.Serialize(id, Opts);
            await Assert.That(json).IsEqualTo("\"5560160680\"");
        }

        [Test]
        public async Task WritesLongFormatString_ForEnskildFirma()
        {
            // Enskild firma orgnummer reuses a personnummer; LongFormat always returns 10 digits.
            var id = OrganisationId.Parse("189001019802");
            var json = JsonSerializer.Serialize(id, Opts);
            await Assert.That(json).IsEqualTo("\"9001019802\"");
        }
    }

    public class Deserialize
    {
        [Test]
        public async Task ParsesTenDigitString()
        {
            var id = JsonSerializer.Deserialize<OrganisationId>("\"5560160680\"", Opts);
            await Assert.That(id).IsNotNull();
            await Assert.That(id!.LongFormat()).IsEqualTo("5560160680");
        }

        [Test]
        public async Task ParsesEnskildFirmaPersonnummer()
        {
            var id = JsonSerializer.Deserialize<OrganisationId>("\"189001019802\"", Opts);
            await Assert.That(id).IsNotNull();
            await Assert.That(id!.LongFormat()).IsEqualTo("9001019802");
        }

        [Test]
        public async Task ThrowsJsonException_OnNull()
        {
            await Assert.That(() => JsonSerializer.Deserialize<OrganisationId>("null", Opts))
                .Throws<JsonException>();
        }

        [Test]
        public async Task ThrowsException_OnMalformed()
        {
            await Assert.That(() => JsonSerializer.Deserialize<OrganisationId>("\"not-an-org\"", Opts))
                .ThrowsException();
        }
    }

    public class RoundTrip
    {
        [Test]
        [Arguments("5560160680")]
        [Arguments("556016-0680")]
        public async Task RoundTrips(string input)
        {
            var original = OrganisationId.Parse(input);
            var json = JsonSerializer.Serialize(original, Opts);
            var roundTripped = JsonSerializer.Deserialize<OrganisationId>(json, Opts);
            await Assert.That(roundTripped).IsEqualTo(original);
        }
    }
}
