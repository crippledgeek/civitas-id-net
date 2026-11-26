namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>
///     How one entry point that does not throw answers a text: whether it accepts it, and whether it left a result
///     in its out parameter, or <see langword="null" /> where it has none. A rejecting entry point must leave none.
/// </summary>
internal readonly record struct ProbeAnswer(string Name, bool Accepted, bool? LeftResult);
