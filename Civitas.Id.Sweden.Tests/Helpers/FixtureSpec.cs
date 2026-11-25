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
        DataRows = 10,
        Sha256 = "6019d7905c294a45e7f3a91865a7653c703b8fb9af04cdbe2835c58dc4bb3ba8",
        HasHeader = true
    };

    /// <summary>Official person numbers written as sole-trader organisation numbers. A column that drops the century is left out.</summary>
    public static readonly FixtureSpec OrganisationsnummerEnskildFirma = new()
    {
        FileName = "testorganisationsnummer_enskild_firma.csv",
        DataRows = 20,
        Sha256 = "7e13959a21ef7535ace2e71fe5bfb866848e2cae67243c650db2f901f58cd01c",
        HasHeader = true
    };

    public static readonly FixtureSpec OrganisationsnummerTextual = new()
    {
        FileName = "testorganisationsnummer.txt",
        DataRows = 6,
        Sha256 = "17fe314c5a642c3aabc4774e325e233d174c2306d70ac9b1461b2a3e1999085d",
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

    /// <summary>Skatteverket's published 2000-2009 personnummer set, byte for byte (CRLF).</summary>
    public static readonly FixtureSpec Personnummer2000S = new()
    {
        FileName = "Testpersonnummer_2000-2009_official.csv",
        DataRows = 7323,
        Sha256 = "de273ddbbbbc5c13ff5017230f6d94a65f3df3f70d04b26de0769cf6f8daa50f",
        HasHeader = true
    };

    /// <summary>Skatteverket's published 2020 samordningsnummer set, byte for byte (CRLF).</summary>
    public static readonly FixtureSpec Samordningsnummer2020 = new()
    {
        FileName = "Testsamordningsnummer_2020_official.csv",
        DataRows = 14,
        Sha256 = "4e22aef1f54a2e59f168558281b883f82c9eecd8258bde6efd1126677ac0e288",
        HasHeader = true
    };

    public static readonly IReadOnlyList<FixtureSpec> All =
    [
        Personnummer1890S, PersonnummerFinal, SamordningsnummerCorrected, OrganisationsnummerExtended,
        OrganisationsnummerEnskildFirma, OrganisationsnummerTextual, InvalidIds, SamordningsnummerOfficial,
        Personnummer2000S, Samordningsnummer2020
    ];
}
