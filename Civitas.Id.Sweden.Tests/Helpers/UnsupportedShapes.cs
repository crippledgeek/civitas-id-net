namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>Sorts the published samordningsnummer that no parser takes yet by what is missing from them.</summary>
internal static class UnsupportedShapes
{
    private const int UnknownMonth = 0;
    private const int UnknownDay = 60;
    private const int FirstDay = 61;
    private const int LastDay = 91;
    private const int FirstMonth = 1;
    private const int LastMonth = 12;

    /// <summary>The shape that gives the number no calendar date, or null when it has one.</summary>
    /// <exception cref="InvalidDataException">The number has no calendar date and fits no shape.</exception>
    public static UnsupportedShape? Of(OfficialSamordningsnummer number)
    {
        if (number.CalendarDate is not null) return null;

        var month = number.EncodedMonth;
        var day = number.EncodedDay;
        var dayBeyondUnknown = day is >= FirstDay and <= LastDay;
        var realMonth = month is >= FirstMonth and <= LastMonth;
        return (month, day) switch
        {
            (UnknownMonth, UnknownDay) => UnsupportedShape.MonthAndDayUnknown,
            (UnknownMonth, _) when dayBeyondUnknown => UnsupportedShape.MonthUnknown,
            (_, UnknownDay) when realMonth => UnsupportedShape.DayUnknown,
            _ when realMonth && dayBeyondUnknown => UnsupportedShape.ImpossibleDate,
            _ => throw new InvalidDataException("A samordningsnummer without a calendar date fits no pinned shape.")
        };
    }
}
