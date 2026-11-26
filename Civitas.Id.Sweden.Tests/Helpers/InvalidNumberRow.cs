using Civitas.Id.Sweden.Errors;

namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>A row of the invalid number file, with the reason every parser rejects it for today.</summary>
public sealed record InvalidNumberRow : FileRow
{
    public required string Number { get; init; }
    public required InvalidIdNumberReason Reason { get; init; }

    /// <summary>Whether the digits also fail the check digit, which is not the reason they are rejected for.</summary>
    public required bool FailsChecksum { get; init; }
}
