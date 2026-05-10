using Civitas.Id.Sweden.Format;

namespace Civitas.Id.Sweden.Tests.Format;

public class OrganisationNumberTypeTests
{
    [Test]
    public async Task OrganisationNumberType_HasTwoValues()
    {
        var values = Enum.GetValues<OrganisationNumberType>();
        await Assert.That(values).Contains(OrganisationNumberType.LegalPerson);
        await Assert.That(values).Contains(OrganisationNumberType.PhysicalPerson);
        await Assert.That(values.Length).IsEqualTo(2);
    }
}
