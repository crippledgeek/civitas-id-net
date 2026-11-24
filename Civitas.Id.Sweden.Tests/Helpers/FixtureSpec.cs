namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>
///     Pins one test data file: its name, how many data rows it must yield, its SHA-256 and whether the
///     first line is a header. File name, hash and cells stay <see cref="string" /> because they are
///     opaque test plumbing, not domain values. <see cref="DataRows" /> is a count and <see cref="HasHeader" />
///     a flag, both invariant-free scalars. Public only because TUnit test parameters must be.
/// </summary>
public sealed record FixtureSpec
{
    public required string FileName { get; init; }
    public required int DataRows { get; init; }
    public required string Sha256 { get; init; }
    public required bool HasHeader { get; init; }
}

/// <summary>One data row of a verified fixture: where it came from and its trimmed cells.</summary>
internal sealed record FixtureRow(string FileName, int Line, IReadOnlyList<string> Cells);

/// <summary>Every test data file in <c>Fixtures/</c>, pinned by content hash and row count.</summary>
internal static class Fixtures
{
    public static readonly FixtureSpec Personnummer1890S = new()
    {
        FileName = "Testpersonnummer 1890-1899.csv",
        DataRows = 3653,
        Sha256 = "544c6a697651d07d3651a7610e0606890f696a29c1c15aac0a1b3d6055adcd52",
        HasHeader = false
    };

    public static readonly FixtureSpec PersonnummerFinal = new()
    {
        FileName = "testpersonnummer_final.csv",
        DataRows = 3653,
        Sha256 = "2daa8cbba58538faba0d8e4504e41fd97b584dc9e4bb48b234c1413c45b347b0",
        HasHeader = true
    };

    public static readonly FixtureSpec SamordningsnummerCorrected = new()
    {
        FileName = "Testsamordningsnummer_2019_corrected.csv",
        DataRows = 1306,
        Sha256 = "17206878d7ef1d5abb87873e13bd7279691d9407ee770e995282216f10771fd3",
        HasHeader = true
    };

    public static readonly FixtureSpec OrganisationsnummerExtended = new()
    {
        FileName = "testorganisationsnummer_extended.csv",
        DataRows = 22,
        Sha256 = "b6b2560cab4e5d183435669c4f82e72942c8c8428e81a69ea08526bb70f0011f",
        HasHeader = true
    };

    public static readonly FixtureSpec OrganisationsnummerTextual = new()
    {
        FileName = "testorganisationsnummer.txt",
        DataRows = 10,
        Sha256 = "0e4f3796883d3b18224ef1b71e352a5676a7ca5539fbbcf03bcfac990fdcaa54",
        HasHeader = false
    };

    public static readonly FixtureSpec InvalidIds = new()
    {
        FileName = "invalid_swedish_ids.csv",
        DataRows = 9,
        Sha256 = "5742d30bbce11825101000893124b86a7accdde5a5402be976f4aa11e2d3f693",
        HasHeader = false
    };

    /// <summary>Skatteverket's published 2019 samordningsnummer set, byte for byte (CRLF).</summary>
    public static readonly FixtureSpec SamordningsnummerOfficial = new()
    {
        FileName = "Testsamordningsnummer_2019_official.csv",
        DataRows = 1498,
        Sha256 = "54e3fe8b4bc9d6f51b67139266fb284f251af0003b9e3a152873c6ca570af838",
        HasHeader = true
    };

    public static readonly IReadOnlyList<FixtureSpec> All =
    [
        Personnummer1890S, PersonnummerFinal, SamordningsnummerCorrected, OrganisationsnummerExtended,
        OrganisationsnummerTextual, InvalidIds, SamordningsnummerOfficial
    ];
}
