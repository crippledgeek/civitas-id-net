using Civitas.Id.Sweden.Format;

namespace Civitas.Id.Sweden.Tests.Format;

public class PnrFormatTests
{
    [Test]
    public async Task Enum_HasSixCanonicalValues()
    {
        var values = Enum.GetValues<PnrFormat>();
        await Assert.That(values).Contains(PnrFormat.LongFormat);
        await Assert.That(values).Contains(PnrFormat.LongFormatWithSeparator);
        await Assert.That(values).Contains(PnrFormat.LongFormatWithStandardSeparator);
        await Assert.That(values).Contains(PnrFormat.ShortFormat);
        await Assert.That(values).Contains(PnrFormat.ShortFormatWithSeparator);
        await Assert.That(values).Contains(PnrFormat.ShortFormatWithStandardSeparator);
        await Assert.That(values.Length).IsEqualTo(6);
    }
}
