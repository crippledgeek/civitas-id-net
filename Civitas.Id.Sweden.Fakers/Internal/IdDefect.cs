namespace Civitas.Id.Sweden.Fakers.Internal;

/// <summary>The one rule a deliberately invalid number breaks.</summary>
internal enum IdDefect
{
    /// <summary>The last digit does not match the others.</summary>
    WrongCheckDigit,

    /// <summary>The month is 13–19: too high for a date, too low for a legal person.</summary>
    ImpossibleMonth,

    /// <summary>The day is in no range that a personnummer or a samordningsnummer uses.</summary>
    ImpossibleDay,

    /// <summary>29 February in a year without one.</summary>
    NonLeapFebruary29,

    /// <summary>One digit is missing.</summary>
    TooShort,

    /// <summary>There is one digit too many.</summary>
    TooLong,

    /// <summary>One digit is written with a digit character outside ASCII.</summary>
    NonAsciiDigit,

    /// <summary>A separator stands anywhere before its legal position.</summary>
    MisplacedSeparator
}
