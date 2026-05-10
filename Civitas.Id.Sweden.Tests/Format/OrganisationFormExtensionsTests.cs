using Civitas.Id.Sweden.Format;

namespace Civitas.Id.Sweden.Tests.Format;

public class OrganisationFormExtensionsTests
{
    [Test]
    public async Task Code_ReturnsUnderlyingValue()
    {
        await Assert.That(OrganisationForm.AktiebolagOvriga.Code).IsEqualTo(49);
        await Assert.That(OrganisationForm.None.Code).IsEqualTo(0);
        await Assert.That(OrganisationForm.JuridiskFormEjUtredd.Code).IsEqualTo(99);
    }

    [Test]
    public async Task Description_NoneIsPhysicalPerson()
    {
        var desc = OrganisationForm.None.Description;
        await Assert.That(desc).Contains("Fysisk person");
    }

    [Test]
    public async Task Description_AktiebolagOvriga()
    {
        await Assert.That(OrganisationForm.AktiebolagOvriga.Description).IsEqualTo("Övriga aktiebolag");
    }

    [Test]
    public async Task NumberType_NoneIsPhysical()
    {
        await Assert.That(OrganisationForm.None.NumberType).IsEqualTo(OrganisationNumberType.PhysicalPerson);
    }

    [Test]
    public async Task NumberType_AnyOtherFormIsLegal()
    {
        await Assert.That(OrganisationForm.AktiebolagOvriga.NumberType).IsEqualTo(OrganisationNumberType.LegalPerson);
        await Assert.That(OrganisationForm.JuridiskFormEjUtredd.NumberType)
            .IsEqualTo(OrganisationNumberType.LegalPerson);
    }

    [Test]
    public async Task FromCode_KnownCode_ReturnsForm()
    {
        await Assert.That(OrganisationFormExtensions.FromCode(49)).IsEqualTo(OrganisationForm.AktiebolagOvriga);
    }

    [Test]
    public async Task FromCode_UnknownCode_ReturnsNull()
    {
        await Assert.That(OrganisationFormExtensions.FromCode(999)).IsNull();
        await Assert.That(OrganisationFormExtensions.FromCode(20)).IsNull();
    }

    [Test]
    public async Task FromOrganisationNumber_TenDigit_ExtractsForm()
    {
        var form = OrganisationFormExtensions.FromOrganisationNumber("4920000149");
        await Assert.That(form).IsEqualTo(OrganisationForm.AktiebolagOvriga);
    }

    [Test]
    public async Task FromOrganisationNumber_TwelveDigitWithSixteenPrefix_StripsPrefix()
    {
        var form = OrganisationFormExtensions.FromOrganisationNumber("164920000149");
        await Assert.That(form).IsEqualTo(OrganisationForm.AktiebolagOvriga);
    }

    [Test]
    public async Task FromOrganisationNumber_WithSeparator_StripsIt()
    {
        var form = OrganisationFormExtensions.FromOrganisationNumber("492000-0149");
        await Assert.That(form).IsEqualTo(OrganisationForm.AktiebolagOvriga);
    }

    [Test]
    public async Task FromOrganisationNumber_UnknownCode_ReturnsJuridiskFormEjUtredd()
    {
        // First two digits "20" — not in the form code list (codes start at 21)
        var form = OrganisationFormExtensions.FromOrganisationNumber("2020000149");
        await Assert.That(form).IsEqualTo(OrganisationForm.JuridiskFormEjUtredd);
    }

    [Test]
    public async Task FromOrganisationNumber_NullInput_ThrowsArgumentNullException()
    {
        await Assert.That(() => OrganisationFormExtensions.FromOrganisationNumber(null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task FromOrganisationNumber_PlusSeparator_StripsIt()
    {
        var form = OrganisationFormExtensions.FromOrganisationNumber("492000+0149");
        await Assert.That(form).IsEqualTo(OrganisationForm.AktiebolagOvriga);
    }

    [Test]
    public async Task FromOrganisationNumber_SpaceSeparator_StripsIt()
    {
        var form = OrganisationFormExtensions.FromOrganisationNumber("4920000 149");
        await Assert.That(form).IsEqualTo(OrganisationForm.AktiebolagOvriga);
    }

    [Test]
    public async Task FromOrganisationNumber_EmptyString_ReturnsJuridiskFormEjUtredd()
    {
        var form = OrganisationFormExtensions.FromOrganisationNumber("");
        await Assert.That(form).IsEqualTo(OrganisationForm.JuridiskFormEjUtredd);
    }

    [Test]
    public async Task FromOrganisationNumber_TwelveDigitWithoutSixteenPrefix_ReturnsJuridiskFormEjUtredd()
    {
        // 12 digits but starts with "19" (personnummer-style century), not "16" — rejected
        var form = OrganisationFormExtensions.FromOrganisationNumber("194920000149");
        await Assert.That(form).IsEqualTo(OrganisationForm.JuridiskFormEjUtredd);
    }

    public class DescriptionExtension
    {
        public static IEnumerable<OrganisationForm> AllForms()
        {
            return Enum.GetValues<OrganisationForm>();
        }

        [Test]
        [MethodDataSource(nameof(AllForms))]
        public async Task Description_ForEveryDefinedForm_ReturnsNonEmptySwedishString(OrganisationForm form)
        {
            var description = form.Description;

            await Assert.That(description).IsNotNull();
            await Assert.That(description).IsNotEmpty();
        }

        [Test]
        public async Task Description_ForUndeclaredCastInt_ReturnsFallbackString()
        {
            // CA1065-defensive fallback path: forced cast of an undeclared int.
            var fakeForm = (OrganisationForm)9999;

            var description = fakeForm.Description;

            await Assert.That(description).IsEqualTo("Juridisk form ej utredd");
        }

        [Test]
        [Arguments(OrganisationForm.None, "Ingen organisationsform - Fysisk person")]
        [Arguments(OrganisationForm.Bankaktiebolag, "Bankaktiebolag")]
        [Arguments(OrganisationForm.AktiebolagOvriga, "Övriga aktiebolag")]
        [Arguments(OrganisationForm.IdeellaForeningar, "Ideella föreningar")]
        [Arguments(OrganisationForm.JuridiskFormEjUtredd, "Juridisk form ej utredd")]
        public async Task Description_ForKnownForm_ReturnsExactSwedishText(OrganisationForm form, string expected)
        {
            await Assert.That(form.Description).IsEqualTo(expected);
        }
    }
}
