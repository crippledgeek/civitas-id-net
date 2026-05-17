namespace Civitas.Id.Sweden.Tests.TypeConverters;

using System.ComponentModel;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.TypeConverters;

public class AttributeDiscoveryTests
{
    [Test]
    public async Task PersonalId_ResolvesPersonalIdTypeConverter()
    {
        var converter = TypeDescriptor.GetConverter(typeof(PersonalId));
        await Assert.That(converter).IsTypeOf<PersonalIdTypeConverter>();
    }

    [Test]
    public async Task CoordinationId_ResolvesCoordinationIdTypeConverter()
    {
        var converter = TypeDescriptor.GetConverter(typeof(CoordinationId));
        await Assert.That(converter).IsTypeOf<CoordinationIdTypeConverter>();
    }

    [Test]
    public async Task OrganisationId_ResolvesOrganisationIdTypeConverter()
    {
        var converter = TypeDescriptor.GetConverter(typeof(OrganisationId));
        await Assert.That(converter).IsTypeOf<OrganisationIdTypeConverter>();
    }
}
