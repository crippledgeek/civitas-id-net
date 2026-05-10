using System.Globalization;
using Civitas.Id.Sweden.Errors;
using Civitas.Id.Sweden.Internal;

namespace Civitas.Id.Sweden.Fakers.Internal;

/// <summary>
///     Shared arithmetic primitives for the four Swedish ID fakers.
///     Internal — not part of the public API.
/// </summary>
internal static class Generation
{
    /// <summary>
    ///     Sibling-parity birth-date range: 1970-01-01 inclusive,
    ///     2020-01-01 exclusive (last valid year is 2019).
    /// </summary>
    private const int FakerMinYearInclusive = 1970;

    /// <summary>The exclusive upper-bound year. Last actually-generated year: 2019.</summary>
    private const int FakerMaxYearExclusive = 2020;

    /// <summary>
    ///     Constructs a <see cref="DateOnly" /> from the given components, translating
    ///     <see cref="ArgumentOutOfRangeException" /> from the constructor into
    ///     <see cref="InvalidIdNumberException" /> with
    ///     <see cref="InvalidIdNumberReason.InvalidDate" />.
    /// </summary>
    /// <param name="year">Calendar year.</param>
    /// <param name="month">Calendar month (1–12).</param>
    /// <param name="day">Calendar day of month.</param>
    /// <returns>The constructed date.</returns>
    /// <exception cref="InvalidIdNumberException">
    ///     Thrown when the components do not form a valid calendar date.
    /// </exception>
    public static DateOnly ParseDateOrThrow(int year, int month, int day)
    {
        try
        {
            return new DateOnly(year, month, day);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            throw new InvalidIdNumberException(
                $"{year:D4}-{month:D2}-{day:D2}",
                InvalidIdNumberReason.InvalidDate,
                ex);
        }
    }

    /// <summary>
    ///     Picks a uniformly-random valid <see cref="DateOnly" /> in
    ///     [<see cref="FakerMinYearInclusive" />, <see cref="FakerMaxYearExclusive" />).
    /// </summary>
    /// <param name="next">A delegate matching <c>(minInclusive, maxExclusive) =&gt; int</c>; the random source.</param>
    /// <returns>A random birth date within the sibling-parity range.</returns>
    public static DateOnly RandomBirthDate(Func<int, int, int> next)
    {
        ArgumentNullException.ThrowIfNull(next);
        var minDay = new DateOnly(FakerMinYearInclusive, 1, 1).DayNumber;
        var maxDay = new DateOnly(FakerMaxYearExclusive, 1, 1).DayNumber;
        var day = next(minDay, maxDay);
        return DateOnly.FromDayNumber(day);
    }

    /// <summary>
    ///     Picks a uniformly-random valid <see cref="DateOnly" /> for a person
    ///     who would be 100–110 years old (inclusive) as of <paramref name="today" />.
    /// </summary>
    /// <param name="next">A delegate matching <c>(minInclusive, maxExclusive) =&gt; int</c>; the random source.</param>
    /// <param name="today">The reference "today" used to compute the age window.</param>
    /// <returns>A 1 January birth date in the centenarian range.</returns>
    public static DateOnly RandomCentenarianBirthDate(Func<int, int, int> next, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(next);
        var ageYears = next(100, 111);
        var year = today.Year - ageYears;
        return new DateOnly(year, 1, 1);
    }

    /// <summary>
    ///     Returns a single decimal digit. Odd if <paramref name="male" />, even otherwise.
    /// </summary>
    /// <param name="next">A delegate matching <c>(minInclusive, maxExclusive) =&gt; int</c>; the random source.</param>
    /// <param name="male">When <c>true</c>, returns an odd digit; otherwise an even digit.</param>
    /// <returns>A digit in <c>[0, 9]</c> with the requested parity.</returns>
    public static int RandomGenderDigit(Func<int, int, int> next, bool male)
    {
        ArgumentNullException.ThrowIfNull(next);
        var bucket = next(0, 5);
        return male ? bucket * 2 + 1 : bucket * 2;
    }

    /// <summary>Returns a 3-digit serial component (000–998).</summary>
    /// <param name="next">A delegate matching <c>(minInclusive, maxExclusive) =&gt; int</c>; the random source.</param>
    /// <returns>An integer in <c>[0, 998]</c>.</returns>
    public static int RandomSerial(Func<int, int, int> next)
    {
        ArgumentNullException.ThrowIfNull(next);
        return next(0, 999);
    }

    /// <summary>
    ///     Builds a 12-digit personnummer from <paramref name="birth" />, a 3-digit
    ///     <paramref name="serial" /> (000–999) and a single <paramref name="genderDigit" />.
    ///     The Luhn check digit is appended.
    /// </summary>
    /// <param name="birth">The birth date.</param>
    /// <param name="serial">The serial component (000–999).</param>
    /// <param name="genderDigit">The gender digit (0–9).</param>
    /// <returns>A 12-digit personnummer string.</returns>
    public static string BuildPersonnummer(DateOnly birth, int serial, int genderDigit)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(serial, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(serial, 999);
        ArgumentOutOfRangeException.ThrowIfLessThan(genderDigit, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(genderDigit, 9);

        var yyyy = birth.Year.ToString("D4", CultureInfo.InvariantCulture);
        var mm = birth.Month.ToString("D2", CultureInfo.InvariantCulture);
        var dd = birth.Day.ToString("D2", CultureInfo.InvariantCulture);
        var serial2 = (serial / 10).ToString("D2", CultureInfo.InvariantCulture);
        var genderD = genderDigit.ToString(CultureInfo.InvariantCulture);

        var body9 = $"{yyyy[2..]}{mm}{dd}{serial2}{genderD}";
        var check = SwedishLuhnAlgorithm.ComputeCheckDigit(body9);
        return $"{yyyy}{mm}{dd}{serial2}{genderD}{check}";
    }

    /// <summary>
    ///     Builds a 12-digit samordningsnummer from <paramref name="birth" />,
    ///     applying the day+60 encoding. Other parameters as in
    ///     <see cref="BuildPersonnummer" />.
    /// </summary>
    /// <param name="birth">The birth date (the +60 day offset is applied internally).</param>
    /// <param name="serial">The serial component (000–999).</param>
    /// <param name="genderDigit">The gender digit (0–9).</param>
    /// <returns>A 12-digit samordningsnummer string.</returns>
    public static string BuildSamordningsnummer(DateOnly birth, int serial, int genderDigit)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(serial, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(serial, 999);
        ArgumentOutOfRangeException.ThrowIfLessThan(genderDigit, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(genderDigit, 9);

        var yyyy = birth.Year.ToString("D4", CultureInfo.InvariantCulture);
        var mm = birth.Month.ToString("D2", CultureInfo.InvariantCulture);
        var ddPlus60 = (birth.Day + 60).ToString("D2", CultureInfo.InvariantCulture);
        var serial2 = (serial / 10).ToString("D2", CultureInfo.InvariantCulture);
        var genderD = genderDigit.ToString(CultureInfo.InvariantCulture);

        var body9 = $"{yyyy[2..]}{mm}{ddPlus60}{serial2}{genderD}";
        var check = SwedishLuhnAlgorithm.ComputeCheckDigit(body9);
        return $"{yyyy}{mm}{ddPlus60}{serial2}{genderD}{check}";
    }
}
