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

    /// <summary>What the public methods call the clock that <c>today</c> was read from.</summary>
    private const string ClockParameter = "timeProvider";

    private const int YoungestCentenarian = 100;
    private const int OldestCentenarian = 110;

    /// <summary>The first birth date a person's number is generated for.</summary>
    public static readonly DateOnly MinimumBirthDate = new(SwedishIdParsing.MinimumBirthYear, 1, 1);

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

    /// <inheritdoc cref="ParseDateOrThrow" />
    /// <summary>As <see cref="ParseDateOrThrow" />, for a date of birth.</summary>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when the components form a date before <see cref="MinimumBirthDate" />.
    /// </exception>
    public static DateOnly ParseBirthDateOrThrow(int year, int month, int day)
    {
        var date = ParseDateOrThrow(year, month, day);
        ArgumentOutOfRangeException.ThrowIfLessThan(year, MinimumBirthDate.Year);
        return date;
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
    /// <returns>A 1 January birth date in the centenarian range, not before <see cref="MinimumBirthDate" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Everyone of that age on <paramref name="today" /> was born before <see cref="MinimumBirthDate" />.
    /// </exception>
    public static DateOnly RandomCentenarianBirthDate(Func<int, int, int> next, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentOutOfRangeException.ThrowIfLessThan(
            today, MinimumBirthDate.AddYears(YoungestCentenarian), ClockParameter);
        var ageYears = next(YoungestCentenarian, OldestCentenarian + 1);
        // Lowering the age, rather than drawing from fewer, keeps what a seed gives on every other clock.
        var year = Math.Max(today.Year - ageYears, MinimumBirthDate.Year);
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

    /// <summary>Puts the given values into the parts of a personnummer.</summary>
    /// <param name="birth">The birth date.</param>
    /// <param name="serial">The serial component (000–999), of which the last digit is dropped.</param>
    /// <param name="genderDigit">The gender digit (0–9), which takes the place of that digit.</param>
    /// <returns>The parts.</returns>
    public static IdDraft DraftPersonnummer(DateOnly birth, int serial, int genderDigit)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(serial, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(serial, 999);
        ArgumentOutOfRangeException.ThrowIfLessThan(genderDigit, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(genderDigit, 9);

        return new IdDraft
        {
            Century = birth.Year / SwedishIdParsing.YearsPerCentury,
            Year = birth.Year % SwedishIdParsing.YearsPerCentury,
            Month = birth.Month,
            Day = birth.Day,
            Serial = serial - serial % 10 + genderDigit
        };
    }

    /// <summary>
    ///     Puts the given values into the parts of a samordningsnummer, as <see cref="DraftPersonnummer" /> does
    ///     but with the day as a samordningsnummer writes it.
    /// </summary>
    /// <param name="birth">The birth date, without the offset.</param>
    /// <param name="serial">The serial component (000–999).</param>
    /// <param name="genderDigit">The gender digit (0–9).</param>
    /// <returns>The parts.</returns>
    public static IdDraft DraftSamordningsnummer(DateOnly birth, int serial, int genderDigit)
    {
        return DraftPersonnummer(birth, serial, genderDigit) with
        {
            Day = birth.Day + IdDraft.CoordinationDayOffset
        };
    }
}
