namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>
///     Where the columns of a derived person number file are. The number is the first column. The positions are plain
///     <see cref="int" />s: places in a test file, with no domain meaning.
/// </summary>
internal sealed record PersonColumns
{
    /// <summary>The columns of <c>testpersonnummer_final.csv</c>.</summary>
    public static readonly PersonColumns Personnummer = new()
    {
        Female = 1,
        Male = 2,
        Adult = 3,
        Child = 4,
        Age = 5,
        Valid = 6
    };

    /// <summary>The columns of <c>Testsamordningsnummer_2019_corrected.csv</c>, which differ in order.</summary>
    public static readonly PersonColumns Samordningsnummer = new()
    {
        Female = 1,
        Male = 2,
        Age = 3,
        Adult = 4,
        Child = 5,
        Valid = 6
    };

    public required int Female { get; init; }
    public required int Male { get; init; }
    public required int Age { get; init; }
    public required int Adult { get; init; }
    public required int Child { get; init; }
    public required int Valid { get; init; }
}
