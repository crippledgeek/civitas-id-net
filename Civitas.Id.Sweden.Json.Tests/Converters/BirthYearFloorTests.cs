using System.Text.Json;
using System.Text.Json.Serialization;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Json.Converters;

namespace Civitas.Id.Sweden.Json.Tests.Converters;

/// <summary>The first accepted birth year. A number is never printed.</summary>
public class BirthYearFloorTests
{
    private static Func<string, object?> Reading<T>(JsonConverter<T> converter)
        where T : SwedishOfficialId
    {
        var options = new JsonSerializerOptions { Converters = { converter } };
        return text => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(text), options);
    }

    [Test]
    public async Task PersonalIdLongFormat_EarlyYears_AreRejectedAsAWrongCheckDigitIs()
    {
        await BirthYearFloorAssert
            .HoldsAsync(Reading(new PersonalIdLongFormatJsonConverter()), [BirthYearCases.Personnummer])
            .ConfigureAwait(false);
    }

    [Test]
    public async Task PersonalIdShortFormat_EarlyYears_AreRejectedAsAWrongCheckDigitIs()
    {
        await BirthYearFloorAssert
            .HoldsAsync(Reading(new PersonalIdShortFormatJsonConverter()), [BirthYearCases.Personnummer])
            .ConfigureAwait(false);
    }

    [Test]
    public async Task CoordinationIdLongFormat_EarlyYears_AreRejectedAsAWrongCheckDigitIs()
    {
        await BirthYearFloorAssert
            .HoldsAsync(Reading(new CoordinationIdLongFormatJsonConverter()), [BirthYearCases.Samordningsnummer])
            .ConfigureAwait(false);
    }

    [Test]
    public async Task CoordinationIdShortFormat_EarlyYears_AreRejectedAsAWrongCheckDigitIs()
    {
        await BirthYearFloorAssert
            .HoldsAsync(Reading(new CoordinationIdShortFormatJsonConverter()), [BirthYearCases.Samordningsnummer])
            .ConfigureAwait(false);
    }

    [Test]
    public async Task SwedishOfficialIdLongFormat_EarlyYears_AreRejectedAsAWrongCheckDigitIs()
    {
        await BirthYearFloorAssert
            .HoldsAsync(Reading(new SwedishOfficialIdLongFormatJsonConverter()), BirthYearCases.All)
            .ConfigureAwait(false);
    }

    [Test]
    public async Task SwedishOfficialIdShortFormat_EarlyYears_AreRejectedAsAWrongCheckDigitIs()
    {
        await BirthYearFloorAssert
            .HoldsAsync(Reading(new SwedishOfficialIdShortFormatJsonConverter()), BirthYearCases.All)
            .ConfigureAwait(false);
    }

    [Test]
    public async Task OrganisationId_EarlyYears_AreRejectedAsAWrongCheckDigitIs()
    {
        await BirthYearFloorAssert
            .HoldsAsync(Reading(new OrganisationIdJsonConverter()), BirthYearCases.All)
            .ConfigureAwait(false);
    }
}
