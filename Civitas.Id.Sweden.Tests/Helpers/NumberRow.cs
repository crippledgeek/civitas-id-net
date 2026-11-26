namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>A row of a file that holds one number per line, written as the file writes it.</summary>
public sealed record NumberRow : FileRow
{
    public required string Number { get; init; }
}
