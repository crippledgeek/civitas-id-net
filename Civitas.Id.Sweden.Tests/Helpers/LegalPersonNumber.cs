namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>
///     The organisation number of a legal person, read from the pinned organisation file, with the written forms the
///     tests need. The forms are <see cref="string" /> because they are raw parser input and expected text.
/// </summary>
internal sealed record LegalPersonNumber
{
    private const int Length = 10;
    private const int SerialLength = 4;

    public LegalPersonNumber(string digits)
    {
        if (digits.Length != Length || !digits.All(char.IsAsciiDigit))
            throw new ArgumentException($"A legal person's number is {Length} digits.", nameof(digits));
        Digits = digits;
    }

    /// <summary>NNNNNNNNNN.</summary>
    public string Digits { get; }

    /// <summary>NNNNNN-NNNN.</summary>
    public string WithHyphen => Digits[..^SerialLength] + '-' + Digits[^SerialLength..];

    /// <summary>The legacy form with the prefix 16.</summary>
    public string WithSixteenPrefix => "16" + Digits;

    /// <summary>The number after a person's century, which no legal person has.</summary>
    public string WithPersonCentury => "19" + Digits;

    /// <summary>The number with another check digit, so a number that is not valid.</summary>
    public string WithWrongCheckDigit => CheckDigits.Replaced(Digits);
}
