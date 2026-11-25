using System.Text.Json;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Fakers;
using Civitas.Id.Sweden.Json.Converters;

namespace Civitas.Id.Sweden.Json.Tests.Converters;

public class PersonalIdShortFormatJsonConverterTests
{
    private static readonly JsonSerializerOptions Opts = new()
    {
        Converters = { new PersonalIdShortFormatJsonConverter() }
    };

    // No pinned set holds a personnummer of 1926-1999, the years whose short form resolves to the 1900s.
    private static readonly DateOnly BornInTheNineteenHundreds = new(1981, 12, 18);

    private static PersonalId TwentiethCenturyPerson()
    {
        return PersonalIdFaker.WithSeed(1).Generate(BornInTheNineteenHundreds);
    }

    public class Serialize
    {
        [Test]
        public async Task WritesShortFormatString_TenDigitsNoSeparator()
        {
            // ShortFormat = YYMMDDXXXX (10 digits, no separator), never a "+" centenarian sentinel.
            var id = PersonalId.Parse(OfficialNumbers.Modern.Long);
            var json = JsonSerializer.Serialize(id, Opts);
            await Assert.That(json).IsEqualTo($"\"{OfficialNumbers.Modern.Short}\"");
        }

        [Test]
        public async Task WritesShortFormat_For1890s_TenDigitsNoSeparator()
        {
            // Even for centenarians the converter emits 10-digit no-separator form
            // (PnrFormat.ShortFormat strips chars[2..] of the canonical 12-digit form).
            var id = PersonalId.Parse(OfficialNumbers.Centenarian.Long);
            var json = JsonSerializer.Serialize(id, Opts);
            await Assert.That(json).IsEqualTo($"\"{OfficialNumbers.Centenarian.Short}\"");
        }
    }

    public class Deserialize
    {
        [Test]
        public async Task ParsesLongFormatString()
        {
            var id = JsonSerializer.Deserialize<PersonalId>($"\"{OfficialNumbers.Modern.Long}\"", Opts);
            await Assert.That(id).IsNotNull();
            await Assert.That(id!.LongFormat()).IsEqualTo(OfficialNumbers.Modern.Long);
        }

        [Test]
        public async Task ParsesShortFormatString()
        {
            var id = JsonSerializer.Deserialize<PersonalId>($"\"{OfficialNumbers.Modern.Short}\"", Opts);
            await Assert.That(id).IsNotNull();
            await Assert.That(id!.LongFormat()).IsEqualTo(OfficialNumbers.Modern.Long);
        }

        [Test]
        public async Task ParsesShortFormatString_OfTheNineteenHundreds()
        {
            var person = TwentiethCenturyPerson();
            var id = JsonSerializer.Deserialize<PersonalId>($"\"{person.LongFormat()[2..]}\"", Opts);
            await Assert.That(id).IsEqualTo(person);
            await Assert.That(id!.BirthDate).IsEqualTo(BornInTheNineteenHundreds);
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
        public static IEnumerable<string> RoundTripsInputs()
        {
            yield return OfficialNumbers.Modern.Long;
            yield return OfficialNumbers.EighteenIn2026.Long;
            yield return TwentiethCenturyPerson().LongFormat();
        }

        [Test]
        [MethodDataSource(nameof(RoundTripsInputs))]
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
            await Assert.That(roundTripped!.BirthDate).IsEqualTo(original.BirthDate);
        }
    }
}
