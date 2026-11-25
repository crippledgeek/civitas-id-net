namespace Civitas.Id.Sweden.EntityFrameworkCore.Tests.Converters;

/// <summary>The first accepted birth year. A number is never printed.</summary>
public class BirthYearFloorTests
{
    [Test]
    public async Task PersonalId_EarlyYears_AreRejectedAsAWrongCheckDigitIs()
    {
        await BirthYearFloorAssert
            .HoldsAsync(new PersonalIdConverter().ConvertFromProvider, [BirthYearCases.Personnummer])
            .ConfigureAwait(false);
    }

    [Test]
    public async Task CoordinationId_EarlyYears_AreRejectedAsAWrongCheckDigitIs()
    {
        await BirthYearFloorAssert
            .HoldsAsync(new CoordinationIdConverter().ConvertFromProvider, [BirthYearCases.Samordningsnummer])
            .ConfigureAwait(false);
    }

    [Test]
    public async Task OrganisationId_EarlyYears_AreRejectedAsAWrongCheckDigitIs()
    {
        await BirthYearFloorAssert
            .HoldsAsync(new OrganisationIdConverter().ConvertFromProvider, BirthYearCases.All)
            .ConfigureAwait(false);
    }
}
