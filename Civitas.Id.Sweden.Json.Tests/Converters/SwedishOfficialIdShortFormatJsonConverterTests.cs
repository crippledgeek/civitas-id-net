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
        public static IEnumerable<(string Input, Type ExpectedType)> MaterializesCorrectSubtypeInputs()
        {
            yield return (OfficialNumbers.Modern.Long, typeof(PersonalId));
            yield return (OfficialNumbers.CentenarianSamordningsnummer.Long, typeof(CoordinationId));
            yield return (LegalPersonNumbers.Aktiebolag.Digits, typeof(OrganisationId));
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
        public async Task WritesShortFormat_ForPersonalId()
        {
            // PersonalId is written using PnrFormat.ShortFormat = 10-digit no-separator.
            SwedishOfficialId id = PersonalId.Parse(OfficialNumbers.Modern.Long);
            var json = JsonSerializer.Serialize(id, Opts);
            await Assert.That(json).IsEqualTo($"\"{OfficialNumbers.Modern.Short}\"");
        }

        [Test]
        public async Task WritesShortFormat_ForCoordinationId()
        {
            // CoordinationId is also written as 10-digit no-separator short form.
            SwedishOfficialId id = CoordinationId.Parse(OfficialNumbers.CentenarianSamordningsnummer.Long);
            var json = JsonSerializer.Serialize(id, Opts);
            await UnprintedAssert.AreEqual(json, $"\"{OfficialNumbers.CentenarianSamordningsnummer.Short}\"")
                .ConfigureAwait(false);
        }

        [Test]
        public async Task WritesLongFormat_ForOrganisationId()
        {
            // OrganisationId has no ShortFormat axis; always written as 10-digit LongFormat.
            SwedishOfficialId id = OrganisationId.Parse(LegalPersonNumbers.Aktiebolag.Digits);
            var json = JsonSerializer.Serialize(id, Opts);
            await Assert.That(json).IsEqualTo($"\"{LegalPersonNumbers.Aktiebolag.Digits}\"");
        }
    }

    public class RoundTrip
    {
        public static IEnumerable<string> RoundTripsInputs()
        {
            yield return OfficialNumbers.Modern.Long; // PersonalId — modern, round-trips via 10-digit ShortFormat
            yield return OfficialNumbers.TwentiethCenturySamordningsnummer.Long;
            yield return OfficialNumbers.ModernSamordningsnummer.Long; // CoordinationId — modern, round-trips
            yield return LegalPersonNumbers.Aktiebolag.Digits; // OrganisationId — always 10-digit LongFormat, always round-trips
        }

        [Test]
        [MethodDataSource(nameof(RoundTripsInputs))]
        public async Task RoundTrips(string input)
        {
            var original = SwedishOfficialId.ParseAny(input);
            var json = JsonSerializer.Serialize(original, Opts);
            var roundTripped = JsonSerializer.Deserialize<SwedishOfficialId>(json, Opts);
            await Assert.That(roundTripped).IsEqualTo(original);
        }

        [Test]
        public async Task RoundTrips_PersonOfTheNineteenHundreds()
        {
            SwedishOfficialId original = GeneratedNumbers.Person(GeneratedNumbers.TwentiethCentury);
            var json = JsonSerializer.Serialize(original, Opts);
            var roundTripped = JsonSerializer.Deserialize<SwedishOfficialId>(json, Opts);
            await UnprintedAssert.AreEqual(roundTripped, original).ConfigureAwait(false);
            await Assert.That((roundTripped as PersonalId)?.BirthDate).IsEqualTo(GeneratedNumbers.TwentiethCentury);
        }
    }
}
