namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>
///     Why a published samordningsnummer encodes no calendar date, and so is not yet taken by any parser. The months
///     and days are those of the number as published, where the day part is the calendar day plus 60.
/// </summary>
public enum UnsupportedShape
{
    /// <summary>Month 00 and a day part above 60: the month is not known.</summary>
    MonthUnknown,

    /// <summary>A real month and day part 60: the day is not known.</summary>
    DayUnknown,

    /// <summary>Month 00 and day part 60: neither is known.</summary>
    MonthAndDayUnknown,

    /// <summary>A real month and a day part above 60 that the month does not have.</summary>
    ImpossibleDate
}
