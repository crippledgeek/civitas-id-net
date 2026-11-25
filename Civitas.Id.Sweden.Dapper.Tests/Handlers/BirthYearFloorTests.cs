namespace Civitas.Id.Sweden.Dapper.Tests.Handlers;

/// <summary>The first accepted birth year. A number is never printed.</summary>
public class BirthYearFloorTests
{
    [Test]
    public async Task PersonalId_EarlyYears_AreRejectedAsAWrongCheckDigitIs()
    {
        await BirthYearFloorAssert
            .HoldsAsync(text => PersonalIdHandler.Default.Parse(text), [BirthYearCases.Personnummer])
            .ConfigureAwait(false);
    }

    [Test]
    public async Task CoordinationId_EarlyYears_AreRejectedAsAWrongCheckDigitIs()
    {
        await BirthYearFloorAssert
            .HoldsAsync(text => CoordinationIdHandler.Default.Parse(text), [BirthYearCases.Samordningsnummer])
            .ConfigureAwait(false);
    }

    [Test]
    public async Task OrganisationId_EarlyYears_AreRejectedAsAWrongCheckDigitIs()
    {
        await BirthYearFloorAssert
            .HoldsAsync(text => OrganisationIdHandler.Default.Parse(text), BirthYearCases.All)
            .ConfigureAwait(false);
    }
}
