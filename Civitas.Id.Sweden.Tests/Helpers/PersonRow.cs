namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>
///     A row of a person number file with the columns of the project's derived files: the number, and what it should
///     decode to at the file's reference date. <see cref="Age" /> is a count of years.
/// </summary>
public sealed record PersonRow : FileRow
{
    public required string Number { get; init; }
    public required bool Female { get; init; }
    public required bool Male { get; init; }
    public required int Age { get; init; }
    public required bool Adult { get; init; }
    public required bool Child { get; init; }
}
