using Civitas.Id.Sweden.Internal;

namespace Civitas.Id.Sweden.Tests.Internal;

public class SwedishLuhnAlgorithmTests
{
    [Test]
    [Arguments("811218987", 6)] // 8112189876 — known valid personnummer
    [Arguments("900101980", 2)] // 9001019802 — from testpersonnummer_final.csv (189001019802)
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
        await Assert.That(SwedishLuhnAlgorithm.IsValid("8112189876")).IsTrue();
    }

    [Test]
    public async Task IsValid_RejectsWrongChecksum()
    {
        await Assert.That(SwedishLuhnAlgorithm.IsValid("8112189870")).IsFalse();
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
