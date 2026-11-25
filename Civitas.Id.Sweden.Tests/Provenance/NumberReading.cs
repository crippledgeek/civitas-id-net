using System.Globalization;

namespace Civitas.Id.Sweden.Tests.Provenance;

/// <summary>
///     One way to read a run of digits as an identity number: 12 digits with a century or 10 without. Its date and
///     checksum rules are written here, apart from the library's, so that a library defect cannot hide a number.
/// </summary>
internal sealed record NumberReading
{
    private const int ShortLength = 10;
    private const int LongLength = 12;
    private const int MonthsPerYear = 12;
    private const int FirstCoordinationDay = 60;
    private const int LastCoordinationDay = 91;

    // The doubled digit's own digits summed, indexed by the digit.
    private static readonly int[] DoubledDigitSums = [0, 2, 4, 6, 8, 1, 3, 5, 7, 9];

    private static readonly string[] Centuries = ["18", "19", "20"];

    public NumberReading(string digits)
    {
        if (digits.Length is not (ShortLength or LongLength) || !digits.All(char.IsAsciiDigit))
            throw new ArgumentException("A reading is 10 or 12 digits.", nameof(digits));
        Digits = digits;
    }

    /// <summary>The 10 or 12 digits.</summary>
    public string Digits { get; }

    /// <summary>True for a 12-digit reading whose century a living or recorded person can have.</summary>
    public static bool IsPersonCentury(string century)
    {
        return Centuries.Contains(century, StringComparer.Ordinal);
    }

    /// <summary>True when the date and the checksum both hold for a personnummer or a samordningsnummer.</summary>
    public bool IsPersonShaped()
    {
        var body = Digits[^ShortLength..];
        return HasValidChecksum(body) && (IsCoordinationDate(body) || PossibleYears(body).Any(y => IsDate(y, body)));
    }

    private IEnumerable<int> PossibleYears(string body)
    {
        return Digits.Length == LongLength
            ? [Number(Digits[..4])]
            : Centuries.Select(century => Number(century + body[..2]));
    }

    private static bool IsDate(int year, string body)
    {
        var (month, day) = MonthAndDay(body);
        return year > 0 && month is >= 1 and <= MonthsPerYear
                        && day >= 1 && day <= DateTime.DaysInMonth(year, month);
    }

    // Skatteverket issues samordningsnummer with an unknown month (00) or an unknown day (60).
    private static bool IsCoordinationDate(string body)
    {
        var (month, day) = MonthAndDay(body);
        return month <= MonthsPerYear && day is >= FirstCoordinationDay and <= LastCoordinationDay;
    }

    private static (int Month, int Day) MonthAndDay(string body)
    {
        return (Number(body[2..4]), Number(body[4..6]));
    }

    private static bool HasValidChecksum(string body)
    {
        return body.Select((c, i) => i % 2 == 0 ? DoubledDigitSums[c - '0'] : c - '0').Sum() % 10 == 0;
    }

    private static int Number(string digits)
    {
        return int.Parse(digits, NumberStyles.None, CultureInfo.InvariantCulture);
    }
}
