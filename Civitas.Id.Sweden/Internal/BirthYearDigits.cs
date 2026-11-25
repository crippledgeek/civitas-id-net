namespace Civitas.Id.Sweden.Internal;

/// <summary>
///     What an input says about the birth year. The parts are plain numbers, not value objects, because they are
///     the digits as written, which may belong to no possible year.
/// </summary>
internal readonly record struct BirthYearDigits
{
    /// <summary>The first two of twelve digits; absent in the ten-digit form.</summary>
    public required int? ExplicitCentury { get; init; }

    /// <summary>The year without its century.</summary>
    public required int TwoDigitYear { get; init; }

    /// <summary>Whether the separator is a plus, which says that the person is 100 or older.</summary>
    public required bool PlusSeparator { get; init; }
}
