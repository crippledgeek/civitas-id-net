using System.Globalization;
using Civitas.Id.Sweden.Tests.Helpers;

namespace Civitas.Id.Sweden.Tests.Provenance;

public class NumberReadingTests
{
    [Test]
    [Arguments("")]
    [Arguments("12345678901")]
    [Arguments("123456789a")]
    public async Task NeitherTenNorTwelveDigits_Throws(string digits)
    {
        await Assert.That(() => new NumberReading(digits)).Throws<ArgumentException>();
    }

    [Test]
    public async Task OnlyOneCheckDigit_IsAccepted()
    {
        var number = OfficialNumbers.Centenarian;
        var accepted = Enumerable.Range(0, 10)
            .Where(digit => new NumberReading(
                number.Long[..^1] + digit.ToString(CultureInfo.InvariantCulture)).IsPersonShaped())
            .ToList();
        await Assert.That(accepted.SequenceEqual([number.CheckDigit])).IsTrue();
    }
}
