namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>A published samordningsnummer that no parser takes yet, and the shape that makes it so.</summary>
public sealed record UnsupportedRow : FileRow
{
    public required string Number { get; init; }
    public required UnsupportedShape Shape { get; init; }
}
