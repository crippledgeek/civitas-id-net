using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Format;

namespace Civitas.Id.Sweden.Tests.Core;

/// <summary>
///     Tests for cross-conversion methods between PersonalId / CoordinationId / OrganisationId.
/// </summary>
public class CrossConversionTests
{
    // Modern fixtures so round-trip via 10-digit ShortFormat preserves century inference
    // (today >= 2026, sliding window resolves "00" → 2000, "00" coord → 2000).
    private const string ValidPersonalId = "200001019801"; // 2000-01-01, female
    private const string ValidCoordinationId = "200001612399"; // 2000-01-01 (coord day 61), male
    private const string ValidOrganisationId = "5560160680"; // legal person — EuropakooperativEgtsEric

    public class PersonalToOrganisation
    {
        [Test]
        public async Task ToOrganisationId_ReturnsPhysicalPerson()
        {
            var person = PersonalId.Parse(ValidPersonalId);
            var org = person.ToOrganisationId();
            await Assert.That(org.NumberType).IsEqualTo(OrganisationNumberType.PhysicalPerson);
            await Assert.That(org.Form).IsEqualTo(OrganisationForm.None);
        }

        [Test]
        public async Task ToOrganisationId_ShortFormatMatches()
        {
            var person = PersonalId.Parse(ValidPersonalId);
            var org = person.ToOrganisationId();
            await Assert.That(org.ShortFormat()).IsEqualTo(person.ShortFormat());
        }
    }

    public class CoordinationToOrganisation
    {
        [Test]
        public async Task ToOrganisationId_ReturnsPhysicalPerson()
        {
            var coord = CoordinationId.Parse(ValidCoordinationId);
            var org = coord.ToOrganisationId();
            await Assert.That(org.NumberType).IsEqualTo(OrganisationNumberType.PhysicalPerson);
            await Assert.That(org.Form).IsEqualTo(OrganisationForm.None);
        }

        [Test]
        public async Task ToOrganisationId_ShortFormatMatches()
        {
            var coord = CoordinationId.Parse(ValidCoordinationId);
            var org = coord.ToOrganisationId();
            await Assert.That(org.ShortFormat()).IsEqualTo(coord.ShortFormat());
        }
    }

    public class OrganisationToPersonOfficial
    {
        [Test]
        public async Task FromPersonalIdRoundtrip_ReturnsEqualPersonalId()
        {
            var original = PersonalId.Parse(ValidPersonalId);
            var asOrg = original.ToOrganisationId();
            var back = asOrg.ToPhysicalPersonId();
            await Assert.That(back).IsEqualTo(original);
        }

        [Test]
        public async Task FromCoordinationRoundtrip_ReturnsEqualCoordination()
        {
            var original = CoordinationId.Parse(ValidCoordinationId);
            var asOrg = original.ToOrganisationId();
            var back = asOrg.ToPhysicalPersonId();
            await Assert.That(back).IsEqualTo(original);
        }

        [Test]
        public async Task LegalPerson_ReturnsNull()
        {
            // 5560160680 is a real legal-person orgnummer, not derived from any person ID.
            var org = OrganisationId.Parse(ValidOrganisationId);
            var asPerson = org.ToPhysicalPersonId();
            await Assert.That(asPerson).IsNull();
        }
    }
}
