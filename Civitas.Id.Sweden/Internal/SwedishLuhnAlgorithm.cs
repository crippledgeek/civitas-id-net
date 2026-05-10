namespace Civitas.Id.Sweden.Internal;

/// <summary>
///     Luhn checksum calculator for Swedish ID numbers.
///     Operates on the 9-digit core (YYMMDDXXX) to produce the 10th check digit.
/// </summary>
internal static class SwedishLuhnAlgorithm
{
    /// <summary>
    ///     Computes the Luhn check digit for a 9-character all-digit input.
    /// </summary>
    /// <param name="nineDigits">The first 9 digits of a Swedish 10-digit ID number.</param>
    /// <returns>The check digit (0-9) that completes the 10-digit number.</returns>
    /// <exception cref="ArgumentException">When the input is not exactly 9 digits.</exception>
    public static int ComputeCheckDigit(ReadOnlySpan<char> nineDigits)
    {
        if (nineDigits.Length != 9)
            throw new ArgumentException("Input must be exactly 9 digits.", nameof(nineDigits));

        var sum = 0;
        for (var i = 0; i < 9; i++)
        {
            var c = nineDigits[i];
            if (c is < '0' or > '9')
                throw new ArgumentException("Input must contain only digits.", nameof(nineDigits));

            var digit = c - '0';
            // Weights: 2,1,2,1,2,1,2,1,2 (positions 0..8)
            if ((i & 1) == 0)
            {
                digit *= 2;
                if (digit > 9) digit -= 9;
            }

            sum += digit;
        }

        return (10 - sum % 10) % 10;
    }

    /// <summary>
    ///     Validates a 10-character all-digit input where the last digit is the Luhn checksum.
    /// </summary>
    /// <param name="tenDigits">A 10-digit Swedish ID number.</param>
    /// <returns><c>true</c> if the checksum matches; <c>false</c> otherwise (including wrong length or non-digit characters).</returns>
    public static bool IsValid(ReadOnlySpan<char> tenDigits)
    {
        if (tenDigits.Length != 10) return false;
        for (var i = 0; i < 10; i++)
            if (tenDigits[i] is < '0' or > '9')
                return false;

        var expected = ComputeCheckDigit(tenDigits[..9]);
        return expected == tenDigits[9] - '0';
    }
}
