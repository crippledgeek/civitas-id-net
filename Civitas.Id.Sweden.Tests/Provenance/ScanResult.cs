namespace Civitas.Id.Sweden.Tests.Provenance;

/// <summary>
///     The outcome of scanning a set of files: two counts, which have no invariant to guard, and the lines holding a
///     person-shaped number that is in no official set. It never carries a number.
/// </summary>
internal sealed record ScanResult(int FilesScanned, int OfficialHits, IReadOnlyList<SourceLine> Unofficial);
