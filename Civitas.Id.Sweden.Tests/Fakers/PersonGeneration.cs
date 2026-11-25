using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Fakers.Internal;

namespace Civitas.Id.Sweden.Tests.Fakers;

/// <summary>One faker's generating members, so that both person fakers face the same tests.</summary>
internal sealed record PersonGeneration
{
    public required Func<IdValidity, DateOnly?, IdGender?, GeneratedId> Generate { get; init; }
    public required Func<IdDefect, DateOnly?, IdGender?, InvalidGeneratedId> GenerateDefect { get; init; }
    public required IReadOnlyList<IdDefect> SupportedDefects { get; init; }
    public required Func<string, PhysicalPersonId> Parse { get; init; }
}
