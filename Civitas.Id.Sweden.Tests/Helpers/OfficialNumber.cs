using System.Globalization;

namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>
///     A number read from one of Skatteverket's published test sets, with the written forms the tests need. Every form
///     is a slice of the published 12 digits; nothing is computed by the library under test. The forms are
///     <see cref="string" /> because they are raw parser input and expected text.
/// </summary>
internal abstract record OfficialNumber
{
    private const int LongLength = 12;
    private const int CenturyLength = 2;
    private const int SerialLength = 4;

    private protected OfficialNumber(string digits)
    {
        if (digits.Length != LongLength || !digits.All(char.IsAsciiDigit))
            throw new ArgumentException($"An official number is {LongLength} digits.", nameof(digits));
        Long = digits;
    }

    /// <summary>The published form, YYYYMMDDNNNN.</summary>
    public string Long { get; }

    /// <summary>YYMMDDNNNN.</summary>
    public string Short => Long[CenturyLength..];

    /// <summary>YYYYMMDD-NNNN.</summary>
    public string LongWithHyphen => Separated(Long, '-');

    /// <summary>YYYYMMDD+NNNN.</summary>
    public string LongWithPlus => Separated(Long, '+');

    /// <summary>YYMMDD-NNNN.</summary>
    public string ShortWithHyphen => Separated(Short, '-');

    /// <summary>YYMMDD+NNNN.</summary>
    public string ShortWithPlus => Separated(Short, '+');

    /// <summary>The nine digits the check digit is computed over.</summary>
    public string Body => Short[..^1];

    /// <summary>The published check digit, an <see cref="int" /> because the library's check digit is one.</summary>
    public int CheckDigit => Number(Long[^1..]);

    /// <summary>The published form with another check digit, so a number that is not valid.</summary>
    public string WithWrongCheckDigit => CheckDigits.Replaced(Long);

    /// <summary>The last four digits, NNNN.</summary>
    public string Serial => Long[^SerialLength..];

    /// <summary>Whether the number's gender digit, the one before the check digit, is even.</summary>
    public bool IsFemale => (Long[^2] - '0') % 2 == 0;

    /// <summary>The published month part, MM, which is not necessarily a calendar month.</summary>
    public int EncodedMonth => Number(Long[4..6]);

    /// <summary>The published day part, DD, which for a samordningsnummer is the calendar day plus 60.</summary>
    public int EncodedDay => Number(Long[6..8]);

    /// <summary>The date of birth the number encodes, or null when it encodes no calendar date.</summary>
    public DateOnly? CalendarDate =>
        DateOnly.TryParseExact(
            Long[..6] + CalendarDay(EncodedDay).ToString("00", CultureInfo.InvariantCulture),
            "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;

    /// <summary>The date of birth the number encodes.</summary>
    /// <exception cref="InvalidOperationException">The number encodes no calendar date.</exception>
    public DateOnly BirthDate => CalendarDate ?? throw new InvalidOperationException("The number has no calendar date.");

    /// <summary>The published form.</summary>
    public sealed override string ToString()
    {
        return Long;
    }

    /// <summary>The calendar day that the number's day part encodes.</summary>
    private protected abstract int CalendarDay(int encodedDay);

    private static string Separated(string digits, char separator)
    {
        return digits[..^SerialLength] + separator + digits[^SerialLength..];
    }

    private static int Number(string digits)
    {
        return int.Parse(digits, NumberStyles.None, CultureInfo.InvariantCulture);
    }
}
