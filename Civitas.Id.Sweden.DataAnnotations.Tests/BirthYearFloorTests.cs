using System.ComponentModel.DataAnnotations;

namespace Civitas.Id.Sweden.DataAnnotations.Tests;

/// <summary>The first accepted birth year. A number is never printed.</summary>
public class BirthYearFloorTests
{
    private static async Task AssertFloorAsync(ValidationAttribute sut, IReadOnlyList<BirthYearCases> kinds)
    {
        var context = new ValidationContext(new object());
        foreach (var cases in kinds)
        {
            await Assert.That(sut.GetValidationResult(cases.Accepted, context) is null).IsTrue();

            var rejection = sut.GetValidationResult(cases.WithWrongCheckDigit, context);
            await Assert.That(rejection is null).IsFalse();
            foreach (var text in cases.Early)
                await Assert.That(sut.GetValidationResult(text, context)?.ErrorMessage == rejection!.ErrorMessage).IsTrue();
        }
    }

    [Test]
    public async Task PersonalId_EarlyYears_AreRejectedAsAWrongCheckDigitIs()
    {
        await AssertFloorAsync(new ValidPersonalIdAttribute(), [BirthYearCases.Personnummer]).ConfigureAwait(false);
    }

    [Test]
    public async Task CoordinationId_EarlyYears_AreRejectedAsAWrongCheckDigitIs()
    {
        await AssertFloorAsync(new ValidCoordinationIdAttribute(), [BirthYearCases.Samordningsnummer]).ConfigureAwait(false);
    }

    [Test]
    public async Task OrganisationId_EarlyYears_AreRejectedAsAWrongCheckDigitIs()
    {
        await AssertFloorAsync(new ValidOrganisationIdAttribute(), BirthYearCases.All).ConfigureAwait(false);
    }

    [Test]
    public async Task SwedishOfficialId_EarlyYears_AreRejectedAsAWrongCheckDigitIs()
    {
        await AssertFloorAsync(new ValidSwedishOfficialIdAttribute(), BirthYearCases.All).ConfigureAwait(false);
    }
}
