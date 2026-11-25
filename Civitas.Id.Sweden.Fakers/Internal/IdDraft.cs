using System.Globalization;
using Civitas.Id.Sweden.Internal;

namespace Civitas.Id.Sweden.Fakers.Internal;

/// <summary>
///     The parts of a number before its check digit is known. The parts are plain numbers, not value
///     objects, because a draft must be able to hold values that no real number has. They follow the
///     positions the parser reads, so for a legal person <see cref="Year" /> holds the form digits and
///     <see cref="Day" /> the last two digits of the registration year.
/// </summary>
internal readonly record struct IdDraft
{
    /// <summary>What a samordningsnummer adds to the day of birth.</summary>
    public const int CoordinationDayOffset = 60;

    /// <summary>Digits after the separator in its legal position.</summary>
    public const int DigitsAfterSeparator = 4;

    /// <summary>Splits a year into <see cref="Century" /> and <see cref="Year" />.</summary>
    public const int YearsPerCentury = 100;

    private const int MaxTwoDigits = 99;
    private const int MaxThreeDigits = 999;

    /// <summary>The first two of twelve digits; absent in the ten-digit form of a legal person.</summary>
    public required int? Century { get; init; }

    /// <summary>The year without its century.</summary>
    public required int Year { get; init; }

    /// <summary>The month digits.</summary>
    public required int Month { get; init; }

    /// <summary>The day digits as written, so with the offset of a samordningsnummer.</summary>
    public required int Day { get; init; }

    /// <summary>The three digits before the check digit.</summary>
    public required int Serial { get; init; }

    /// <summary>Writes the parts as digits and appends the check digit that matches them.</summary>
    /// <returns>Twelve digits, or ten when there is no <see cref="Century" />.</returns>
    /// <exception cref="ArgumentException">A part does not fit the digits it has.</exception>
    public string Render()
    {
        if (!Fits(Century ?? 0, MaxTwoDigits) || !Fits(Year, MaxTwoDigits) || !Fits(Month, MaxTwoDigits) ||
            !Fits(Day, MaxTwoDigits) || !Fits(Serial, MaxThreeDigits))
            throw new ArgumentException("A part does not fit its digits.");

        var body = string.Create(CultureInfo.InvariantCulture, $"{Year:D2}{Month:D2}{Day:D2}{Serial:D3}");
        var check = SwedishLuhnAlgorithm.ComputeCheckDigit(body);
        return string.Create(CultureInfo.InvariantCulture, $"{Century:D2}{body}{check}");
    }

    /// <summary>The type's name alone: the parts make up a number, which may belong to someone.</summary>
    /// <returns>The name.</returns>
    public override string ToString()
    {
        return nameof(IdDraft);
    }

    private static bool Fits(int value, int max)
    {
        return value >= 0 && value <= max;
    }
}
