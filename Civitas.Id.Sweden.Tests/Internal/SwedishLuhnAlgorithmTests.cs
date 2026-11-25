using Civitas.Id.Sweden.Internal;
using Civitas.Id.Sweden.Tests.Helpers;

namespace Civitas.Id.Sweden.Tests.Internal;

public class SwedishLuhnAlgorithmTests
{
    public static IEnumerable<(string NineDigits, int Expected)> OfficialInputs()
    {
        yield return (OfficialNumbers.Modern.Body, OfficialNumbers.Modern.CheckDigit);
        yield return (OfficialNumbers.Centenarian.Body, OfficialNumbers.Centenarian.CheckDigit);
    }

    [Test]
    [MethodDataSource(nameof(OfficialInputs))]
    [Arguments("000000000", 0)]
    [Arguments("123456789", 7)]
    public async Task ComputeCheckDigit_KnownInputs(string nineDigits, int expected)
    {
        var actual = SwedishLuhnAlgorithm.ComputeCheckDigit(nineDigits);
        await Assert.That(actual).IsEqualTo(expected);
    }

    [Test]
    public async Task ComputeCheckDigit_RejectsWrongLength()
    {
        await Assert.That(() => SwedishLuhnAlgorithm.ComputeCheckDigit("12345678"))
            .Throws<ArgumentException>();
        await Assert.That(() => SwedishLuhnAlgorithm.ComputeCheckDigit("1234567890"))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task ComputeCheckDigit_RejectsNonDigit()
    {
        await Assert.That(() => SwedishLuhnAlgorithm.ComputeCheckDigit("12345678a"))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task IsValid_AcceptsCorrectChecksum()
    {
        await Assert.That(SwedishLuhnAlgorithm.IsValid(OfficialNumbers.Modern.Short)).IsTrue();
    }

    [Test]
    public async Task IsValid_RejectsWrongChecksum()
    {
        await Assert.That(SwedishLuhnAlgorithm.IsValid(CheckDigits.Replaced(OfficialNumbers.Modern.Short))).IsFalse();
    }

    [Test]
    [Arguments("")]
    [Arguments("123")]
    [Arguments("123456789")] // 9 chars — one short
    [Arguments("12345678901")] // 11 chars — one over
    public async Task IsValid_RejectsWrongLength(string input)
    {
        await Assert.That(SwedishLuhnAlgorithm.IsValid(input)).IsFalse();
    }
}
