using System.Globalization;
using Civitas.Id.Sweden.Tests.Helpers;

namespace Civitas.Id.Sweden.Tests.Provenance;

public class NumberReadingTests
{
    [Test]
    [Arguments("")]
    [Arguments("12345678901")]
    [Arguments("12345678901a")]
    public async Task NotTwelveDigits_Throws(string digits)
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

    [Test]
    public async Task ShortForm_WithoutMark_IsInTheLatestCentury()
    {
        var number = OfficialNumbers.TwentiethCenturySamordningsnummer;
        await UnprintedAssert.AreEqual(NumberReading.OfShort(number.Short, false)!.Digits, number.Long)
            .ConfigureAwait(false);
    }

    [Test]
    public async Task ShortForm_OfHundredOrOlder_IsInTheCenturyBefore()
    {
        var number = OfficialNumbers.Centenarian;
        await UnprintedAssert.AreEqual(NumberReading.OfShort(number.Short, true)!.Digits, number.Long)
            .ConfigureAwait(false);
    }

    [Test]
    public async Task OfficialSets_HoldNoBirthYearAfterTheReferenceYear()
    {
        var younger = OfficialNumbers.All.Where(number =>
            number.Long.StartsWith("20", StringComparison.Ordinal)
            && NumberReading.OfShort(number.Short, false)?.Digits != number.Long);
        await Assert.That(younger.Count()).IsEqualTo(0);
    }
}
