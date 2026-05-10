using Civitas.Id.Sweden.Core;

namespace Civitas.Id.Sweden.Tests.Core;

public class SwedishOfficialIdMatcherTests
{
    [Test]
    public async Task TryMatch_NullInput_ReturnsNull()
    {
        await Assert.That(SwedishOfficialId.TryMatchForTesting(null)).IsNull();
    }

    [Test]
    public async Task TryMatch_Empty_ReturnsNull()
    {
        await Assert.That(SwedishOfficialId.TryMatchForTesting("")).IsNull();
        await Assert.That(SwedishOfficialId.TryMatchForTesting("   ")).IsNull();
    }

    [Test]
    public async Task TryMatch_TooLong_ReturnsNull()
    {
        await Assert.That(SwedishOfficialId.TryMatchForTesting(new string('1', 101))).IsNull();
    }

    [Test]
    public async Task TryMatch_TwelveDigit_CapturesCenturyAndDate()
    {
        var m = SwedishOfficialId.TryMatchForTesting("198501019876");
        await Assert.That(m).IsNotNull();
        await Assert.That(m!.HasCentury).IsTrue();
        await Assert.That(m.CenturyValue).IsEqualTo(19);
        await Assert.That(m.Year).IsEqualTo(85);
        await Assert.That(m.Month).IsEqualTo(1);
        await Assert.That(m.Day).IsEqualTo(1);
        await Assert.That(m.Unique).IsEqualTo("9876");
    }

    [Test]
    public async Task TryMatch_TenDigitNoSeparator_CapturesNoCentury()
    {
        var m = SwedishOfficialId.TryMatchForTesting("8501019876");
        await Assert.That(m).IsNotNull();
        await Assert.That(m!.HasCentury).IsFalse();
        await Assert.That(m.HasDelimiter).IsFalse();
    }

    [Test]
    public async Task TryMatch_TenDigitWithSeparator_CapturesDelimiter()
    {
        var m = SwedishOfficialId.TryMatchForTesting("850101-9876");
        await Assert.That(m).IsNotNull();
        await Assert.That(m!.Delimiter).IsEqualTo("-");
    }

    [Test]
    public async Task TryMatch_PlusSeparator_CapturedAsDelimiter()
    {
        var m = SwedishOfficialId.TryMatchForTesting("850101+9876");
        await Assert.That(m).IsNotNull();
        await Assert.That(m!.Delimiter).IsEqualTo("+");
    }

    [Test]
    public async Task TryMatch_NonNumeric_ReturnsNull()
    {
        await Assert.That(SwedishOfficialId.TryMatchForTesting("abcdefghij")).IsNull();
    }

    [Test]
    public async Task TryMatch_WithSePrefix_Matches()
    {
        var m = SwedishOfficialId.TryMatchForTesting("SE198501019876");
        await Assert.That(m).IsNotNull();
        await Assert.That(m!.HasCentury).IsTrue();
        await Assert.That(m.CenturyValue).IsEqualTo(19);
    }

    [Test]
    public async Task TryMatch_LeadingTrailingWhitespace_Matches()
    {
        var m = SwedishOfficialId.TryMatchForTesting("  198501019876  ");
        await Assert.That(m).IsNotNull();
        await Assert.That(m!.Year).IsEqualTo(85);
    }

    [Test]
    public async Task TryMatch_Length100Boundary_FailsRegex()
    {
        // Length is exactly at MaxInputLength (100) — passes the length guard but the regex
        // rejects it because no valid Swedish ID matches 100 characters. Pins the boundary.
        var input = new string('1', 100);
        await Assert.That(SwedishOfficialId.TryMatchForTesting(input)).IsNull();
    }
}
