namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>
///     A row of an organisation number file, with what the file says the number is written as. Cells are raw text,
///     because they are parser input and expected text. A file without a <c>vat_number</c> column leaves it null.
/// </summary>
public sealed record OrganisationRow : FileRow
{
    public required string Input { get; init; }
    public required string LongFormat { get; init; }
    public required string ShortFormat { get; init; }
    public string? VatNumber { get; init; }
    public required string Type { get; init; }
}
