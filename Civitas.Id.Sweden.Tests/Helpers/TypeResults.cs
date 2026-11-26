using Civitas.Id.Sweden.Errors;

namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>How every entry point of one parsed type answers one text: the ones that do not throw, and the ones that do.</summary>
/// <param name="Tries">The entry points that return whether they accept.</param>
/// <param name="Throws">The entry points that throw, with the reason or <see langword="null" /> where they accept.</param>
internal sealed record TypeResults(
    IReadOnlyList<ProbeAnswer> Tries,
    IReadOnlyList<(string Name, InvalidIdNumberReason? Reason)> Throws);
