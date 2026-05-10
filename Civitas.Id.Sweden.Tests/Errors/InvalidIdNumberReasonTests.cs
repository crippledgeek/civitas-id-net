using Civitas.Id.Sweden.Errors;

namespace Civitas.Id.Sweden.Tests.Errors;

public class InvalidIdNumberReasonTests
{
    [Test]
    public async Task InvalidIdNumberReason_UnknownIsTheDefaultValue()
    {
        // Materialise the default through the array indexer to avoid both
        // ConvertToConstant.Local (Rider) and TUnitAssertions0005 (constant subject).
        var value = new[] { default(InvalidIdNumberReason) }[0];
        await Assert.That(value).IsEqualTo(InvalidIdNumberReason.Unknown);
    }

    [Test]
    public async Task InvalidIdNumberReason_HasAllExpectedReasons()
    {
        var values = Enum.GetValues<InvalidIdNumberReason>();
        await Assert.That(values).Contains(InvalidIdNumberReason.Empty);
        await Assert.That(values).Contains(InvalidIdNumberReason.InvalidLength);
        await Assert.That(values).Contains(InvalidIdNumberReason.InvalidFormat);
        await Assert.That(values).Contains(InvalidIdNumberReason.InvalidDate);
        await Assert.That(values).Contains(InvalidIdNumberReason.InvalidChecksum);
        await Assert.That(values).Contains(InvalidIdNumberReason.UnknownOrganisationForm);
        await Assert.That(values).Contains(InvalidIdNumberReason.UnsupportedIdType);
    }
}
