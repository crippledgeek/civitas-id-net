using System.Globalization;

namespace Civitas.Id.Sweden.Tests.Provenance;

/// <summary>
///     Twelve digits read as an identity number. Its date and checksum rules are written here, apart from the
///     library's, so that a library defect cannot hide a number.
/// </summary>
internal sealed record NumberReading
{
    private const int LongLength = 12;
    private const int ShortLength = 10;
    private const int CenturyLength = 2;
    private const int MonthsPerYear = 12;
    private const int FirstCoordinationDay = 60;
    private const int LastCoordinationDay = 91;

    // Fixed, so that a ten-digit number reads the same on every run. It is later than every birth date in the
    // pinned sets; a set with later dates needs a later year here. Ten digits of a person born after it read as
    // the 1900s, which no official number matches except by chance, so such a number is still reported.
    private const int ReferenceYear = 2026;

    // Latest first, the order in which a missing century is tried.
    private static readonly string[] Centuries = ["20", "19", "18"];

    // The doubled digit's own digits summed, indexed by the digit.
    private static readonly int[] DoubledDigitSums = [0, 2, 4, 6, 8, 1, 3, 5, 7, 9];

    public NumberReading(string digits)
    {
        if (digits.Length != LongLength || !digits.All(char.IsAsciiDigit))
            throw new ArgumentException($"A reading is {LongLength} digits.", nameof(digits));
        Digits = digits;
    }

    /// <summary>The 12 digits.</summary>
    public string Digits { get; }

    /// <summary>
    ///     Reads ten digits: in the latest century that gives a person-shaped number, or in the one before that when
    ///     the bearer is marked as 100 or older. Null when no century gives one.
    /// </summary>
    public static NumberReading? OfShort(string digits, bool hundredOrOlder)
    {
        return Centuries
            .Select(century => new NumberReading(century + digits))
            .Where(reading => reading.Year <= ReferenceYear)
            .Skip(hundredOrOlder ? 1 : 0)
            .FirstOrDefault(reading => reading.IsPersonShaped());
    }

    /// <summary>True when the century, date and checksum all hold for a personnummer or a samordningsnummer.</summary>
    public bool IsPersonShaped()
    {
        return Centuries.Contains(Digits[..CenturyLength], StringComparer.Ordinal)
               && HasValidChecksum()
               && (IsCoordinationDate() || IsCalendarDate());
    }

    private int Year => Number(0, 4);

    private int Month => Number(4, 2);

    private int Day => Number(6, 2);

    private bool IsCalendarDate()
    {
        return Month is >= 1 and <= MonthsPerYear && Day >= 1 && Day <= DateTime.DaysInMonth(Year, Month);
    }

    // Skatteverket issues samordningsnummer with an unknown month (00) or an unknown day (60).
    private bool IsCoordinationDate()
    {
        return Month <= MonthsPerYear && Day is >= FirstCoordinationDay and <= LastCoordinationDay;
    }

    private bool HasValidChecksum()
    {
        return Digits[^ShortLength..]
            .Select((c, i) => i % 2 == 0 ? DoubledDigitSums[c - '0'] : c - '0')
            .Sum() % 10 == 0;
    }

    private int Number(int start, int length)
    {
        return int.Parse(Digits.AsSpan(start, length), NumberStyles.None, CultureInfo.InvariantCulture);
    }
}
