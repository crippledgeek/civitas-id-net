namespace Civitas.Id.Sweden.Tests.Provenance;

/// <summary>The outcome of scanning a set of files.</summary>
internal sealed record ScanResult(IReadOnlyList<FileScan> Files)
{
    /// <summary>The official numbers found in all files, a plain count.</summary>
    public int OfficialHits => Files.Sum(file => file.OfficialHits);

    /// <summary>The lines of all files that hold a person-shaped number that is in no official set.</summary>
    public IReadOnlyList<SourceLine> Unofficial => [.. Files.SelectMany(file => file.Unofficial)];
}
