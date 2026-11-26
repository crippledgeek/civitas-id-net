namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>
///     A row that a discovered test takes: the name of the source that fed it, the test method that takes it and the
///     row's place. All three are text because they are names, compared only for equality.
/// </summary>
internal sealed record DiscoveredRow(string SourceName, string TestMethod, string Place);
