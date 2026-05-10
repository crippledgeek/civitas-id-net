using Civitas.Id.Sweden.Format;

namespace Civitas.Id.Sweden.Tests.Format;

public class OrganisationFormTests
{
    [Test]
    public async Task HasThirtyFourValues()
    {
        // 33 official Bolagsverket organisation forms + None sentinel for sole proprietors.
        var count = Enum.GetValues<OrganisationForm>().Length;
        await Assert.That(count).IsEqualTo(34);
    }

    [Test]
    [Arguments(OrganisationForm.None, 0)]
    [Arguments(OrganisationForm.EnklaBolag, 21)]
    [Arguments(OrganisationForm.AktiebolagOvriga, 49)]
    [Arguments(OrganisationForm.EkonomiskaForeningar, 51)]
    [Arguments(OrganisationForm.Bostadsrattsforeningar, 53)]
    [Arguments(OrganisationForm.IdeellaForeningar, 61)]
    [Arguments(OrganisationForm.JuridiskFormEjUtredd, 99)]
    public async Task UnderlyingValueIsTheFormCode(OrganisationForm form, int expectedCode)
    {
        await Assert.That((int)form).IsEqualTo(expectedCode);
    }

    [Test]
    public async Task IsDefined_RecognisesAllListedCodes()
    {
        int[] codes =
        [
            0, 21, 22, 31, 32, 41, 42, 43, 49, 51, 53, 54, 55, 61, 62, 63, 71, 72,
            81, 82, 83, 84, 85, 87, 88, 89, 91, 92, 93, 94, 95, 96, 98, 99
        ];
        foreach (var code in codes) await Assert.That(Enum.IsDefined((OrganisationForm)code)).IsTrue();
    }

    [Test]
    public async Task IsDefined_RejectsUnknownCode()
    {
        await Assert.That(Enum.IsDefined((OrganisationForm)999)).IsFalse();
        await Assert.That(Enum.IsDefined((OrganisationForm)20)).IsFalse();
    }
}
