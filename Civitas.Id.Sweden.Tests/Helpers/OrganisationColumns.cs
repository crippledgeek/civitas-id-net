namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>
///     Where the columns of an organisation number file are. The input is the first column and the long format the
///     second. The positions are plain <see cref="int" />s: places in a test file, with no domain meaning. A file
///     without a short-format or VAT column leaves it null.
/// </summary>
internal sealed record OrganisationColumns
{
    /// <summary>The columns of <c>testorganisationsnummer_extended.csv</c>.</summary>
    public static readonly OrganisationColumns Extended = new() { Short = 2, Valid = 3, Type = 4, Vat = 5 };

    /// <summary>The columns of <c>testorganisationsnummer_enskild_firma.csv</c>.</summary>
    public static readonly OrganisationColumns SoleTrader = new() { Valid = 2, Type = 3 };

    public int? Short { get; init; }
    public required int Valid { get; init; }
    public required int Type { get; init; }
    public int? Vat { get; init; }
}
