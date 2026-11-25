using System.Globalization;
using Civitas.Id.Sweden.Fakers.Internal;

namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>Reads generated numbers and reports on them. It never repairs or builds one.</summary>
internal static class DefectInspection
{
    /// <summary>The parts are positions in the last ten digits, which leave out any century.</summary>
    private const int BodyLength = 10;

    private const int MonthOffset = 2;
    private const int DayOffset = 4;
    private const int PartLength = 2;
    private const int DigitsAfterSeparator = 4;
    private const int YearLength = 4;
    private const int February = 2;

    /// <summary>Written here and not taken from the library, so that a wrong one there is seen.</summary>
    private const int CoordinationDayOffset = 60;

    /// <summary>From a leap year, the years that follow it up to the next.</summary>
    private const int MaxYearShift = 3;

    /// <summary>29 February as a personnummer and as a samordningsnummer write the day.</summary>
    private const int LeapDay = 29;

    private const int CoordinationLeapDay = 89;

    public static readonly IReadOnlyList<int> ImpossibleMonths = [.. Enumerable.Range(13, 7)];
    public static readonly IReadOnlyList<int> ImpossiblePersonalDays = [.. Enumerable.Range(32, 28)];
    public static readonly IReadOnlyList<int> ImpossibleCoordinationDays = [.. Enumerable.Range(92, 8)];

    /// <summary>
    ///     Whether <paramref name="id" /> differs from the valid number it was made from only by its own defect.
    /// </summary>
    public static bool BreaksOnlyItsRule(InvalidGeneratedId id, string valid)
    {
        var text = id.Text;
        return id.Defect switch
        {
            IdDefect.WrongCheckDigit =>
                text.Length == valid.Length && text[..^1] == valid[..^1] && char.IsAsciiDigit(text[^1]) &&
                !IndependentLuhn.IsValid(text),
            IdDefect.ImpossibleMonth => KeepsAllBut(MonthOffset, text, valid) && ImpossibleMonths.Contains(Month(text)),
            IdDefect.ImpossibleDay => KeepsAllBut(DayOffset, text, valid) &&
                                      (IsCoordinationDay(Day(valid))
                                          ? ImpossibleCoordinationDays
                                          : ImpossiblePersonalDays).Contains(Day(text)),
            IdDefect.NonLeapFebruary29 => IsNonLeapFebruary29(text, valid),
            IdDefect.BirthYearBelowMinimum => IsRealDateBelowMinimum(text, valid),
            IdDefect.TooShort => text.Length == valid.Length - 1 && IsSubsequence(text, valid),
            IdDefect.TooLong =>
                text.Length == valid.Length + 1 && text.All(char.IsAsciiDigit) && IsSubsequence(valid, text),
            IdDefect.NonAsciiDigit => HasOneNonAsciiDigit(text, valid),
            IdDefect.MisplacedSeparator =>
                text.Length == valid.Length + 1 && IsSubsequence(valid, text) &&
                text.IndexOf('-', StringComparison.Ordinal) is >= 0 and var index &&
                index < valid.Length - DigitsAfterSeparator,
            _ => false
        };
    }

    public static int Month(string text)
    {
        return Part(MonthOffset, text);
    }

    public static int Day(string text)
    {
        return Part(DayOffset, text);
    }

    public static int FullYear(string text)
    {
        return int.Parse(text.AsSpan(0, YearLength), NumberStyles.None, CultureInfo.InvariantCulture);
    }

    private static int Part(int offset, string text)
    {
        return int.Parse(text.AsSpan(text.Length - BodyLength + offset, PartLength), NumberStyles.None,
            CultureInfo.InvariantCulture);
    }

    /// <summary>Whether only the part at <paramref name="offset" /> and the check digit may differ.</summary>
    private static bool KeepsAllBut(int offset, string text, string valid)
    {
        var start = valid.Length - BodyLength + offset;
        var end = start + PartLength;
        return text.Length == valid.Length && text[..start] == valid[..start] &&
               text[end..^1] == valid[end..^1] && IndependentLuhn.IsValid(text);
    }

    private static bool IsNonLeapFebruary29(string text, string valid)
    {
        if (text.Length != valid.Length) return false;
        var year = FullYear(text);
        var validYear = FullYear(valid);
        var keepsYear = DateTime.IsLeapYear(validYear)
            ? year - validYear is >= 1 and <= MaxYearShift
            : year == validYear;
        return keepsYear && Month(text) == February &&
               Day(text) == (IsCoordinationDay(Day(valid)) ? CoordinationLeapDay : LeapDay) &&
               text[^DigitsAfterSeparator..^1] == valid[^DigitsAfterSeparator..^1] && IndependentLuhn.IsValid(text);
    }

    /// <summary>
    ///     Whether only the year and the check digit may differ, the year is too early, and the date is real. Leap
    ///     years are decided here, because the calendar types throw for year zero.
    /// </summary>
    private static bool IsRealDateBelowMinimum(string text, string valid)
    {
        if (text.Length != valid.Length || !text.All(char.IsAsciiDigit)) return false;
        var year = FullYear(text);
        var isLeapDay = Month(text) == February && Day(text) is LeapDay or CoordinationLeapDay;
        return year < BirthYearCases.FirstAcceptedYear && (!isLeapDay || IsLeapYear(year)) &&
               text[YearLength..^1] == valid[YearLength..^1] && IndependentLuhn.IsValid(text);
    }

    /// <summary>By the Gregorian rule. Year zero is no leap year here, as no calendar type holds its days.</summary>
    private static bool IsLeapYear(int year)
    {
        return year > 0 && (year % 400 == 0 || (year % 4 == 0 && year % 100 != 0));
    }

    private static bool HasOneNonAsciiDigit(string text, string valid)
    {
        return text.Length == valid.Length && text.Count(c => !char.IsAscii(c)) == 1 &&
               text.Zip(valid).All(pair => CharUnicodeInfo.GetDecimalDigitValue(pair.First) == pair.Second - '0');
    }

    private static bool IsCoordinationDay(int day)
    {
        return day > CoordinationDayOffset;
    }

    private static bool IsSubsequence(string part, string whole)
    {
        var matched = whole.Aggregate(0, (count, c) => count < part.Length && part[count] == c ? count + 1 : count);
        return matched == part.Length;
    }
}
