namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>
///     A second implementation of the checksum, so that a defect in the library's cannot hide a number or approve a
///     generated one. It only verifies; it never computes a check digit.
/// </summary>
internal static class IndependentLuhn
{
    private const int DigitsCovered = 10;

    /// <summary>What a doubled digit contributes: the sum of the digits of twice the value.</summary>
    private static readonly int[] Doubled = [0, 2, 4, 6, 8, 1, 3, 5, 7, 9];

    /// <summary>Whether the last ten characters are ASCII digits whose weighted sum is a multiple of ten.</summary>
    public static bool IsValid(ReadOnlySpan<char> number)
    {
        if (number.Length < DigitsCovered) return false;

        var sum = 0;
        var doubled = true;
        foreach (var c in number[^DigitsCovered..])
        {
            if (!char.IsAsciiDigit(c)) return false;
            var value = c - '0';
            sum += doubled ? Doubled[value] : value;
            doubled = !doubled;
        }

        return sum % DigitsCovered == 0;
    }
}
