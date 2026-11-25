// Linked into several test projects, each of which uses only some of the members.
// ReSharper disable MemberCanBePrivate.Global, UnusedMember.Global

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
    private const int CheckDigitLength = 1;

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
    public string Body => Short[..^CheckDigitLength];

    /// <summary>The published check digit.</summary>
    public int CheckDigit => Number(Long[^CheckDigitLength..]);

    /// <summary>The last four digits, NNNN.</summary>
    public string Serial => Long[^SerialLength..];

    /// <summary>The date of birth the number encodes.</summary>
    public DateOnly BirthDate => new(Number(Long[..4]), Number(Long[4..6]), CalendarDay(Number(Long[6..8])));

    /// <summary>False for a number whose day or month part is not a calendar date.</summary>
    public bool HasCalendarDate =>
        DateOnly.TryParseExact(
            Long[..6] + CalendarDay(Number(Long[6..8])).ToString("00", CultureInfo.InvariantCulture),
            "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

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
