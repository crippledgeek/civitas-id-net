namespace Civitas.Id.Sweden.Tests.Provenance;

/// <summary>
///     What a scan found in one file: how many official numbers, a plain count, and the lines that hold a
///     person-shaped number that is in no official set. It never carries a number.
/// </summary>
internal sealed record FileScan(RelativePath Path, int OfficialHits, IReadOnlyList<SourceLine> Unofficial);
