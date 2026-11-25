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
            var id = OrganisationId.Parse(LegalPersonNumbers.Aktiebolag.Digits);
            var json = JsonSerializer.Serialize(id, Opts);
            await Assert.That(json).IsEqualTo($"\"{LegalPersonNumbers.Aktiebolag.Digits}\"");
        }

        [Test]
        public async Task WritesLongFormatString_ForEnskildFirma()
        {
            // Enskild firma orgnummer reuses a personnummer; LongFormat always returns 10 digits.
            var id = OrganisationId.Parse(OfficialNumbers.Centenarian.Long);
            var json = JsonSerializer.Serialize(id, Opts);
            await UnprintedAssert.AreEqual(json, $"\"{OfficialNumbers.Centenarian.Short}\"").ConfigureAwait(false);
        }
    }

    public class Deserialize
    {
        [Test]
        public async Task ParsesTenDigitString()
        {
            var id = JsonSerializer.Deserialize<OrganisationId>($"\"{LegalPersonNumbers.Aktiebolag.Digits}\"", Opts);
            await Assert.That(id).IsNotNull();
            await Assert.That(id!.LongFormat()).IsEqualTo(LegalPersonNumbers.Aktiebolag.Digits);
        }

        [Test]
        public async Task ParsesEnskildFirmaPersonnummer()
        {
            var id = JsonSerializer.Deserialize<OrganisationId>($"\"{OfficialNumbers.Centenarian.Long}\"", Opts);
            await Assert.That(id).IsNotNull();
            await UnprintedAssert.AreEqual(id!.LongFormat(), OfficialNumbers.Centenarian.Short).ConfigureAwait(false);
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
        public static IEnumerable<string> RoundTripsInputs()
        {
            yield return LegalPersonNumbers.Aktiebolag.Digits;
            yield return LegalPersonNumbers.Aktiebolag.WithHyphen;
        }

        [Test]
        [MethodDataSource(nameof(RoundTripsInputs))]
        public async Task RoundTrips(string input)
        {
            var original = OrganisationId.Parse(input);
            var json = JsonSerializer.Serialize(original, Opts);
            var roundTripped = JsonSerializer.Deserialize<OrganisationId>(json, Opts);
            await Assert.That(roundTripped).IsEqualTo(original);
        }
    }
}
