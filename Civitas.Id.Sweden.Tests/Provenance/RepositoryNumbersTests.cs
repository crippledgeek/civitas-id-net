using Civitas.Id.Sweden.Tests.Helpers;

namespace Civitas.Id.Sweden.Tests.Provenance;

/// <summary>A person-shaped number in the repository must be in a pinned official test set.</summary>
public class RepositoryNumbersTests
{
    private static readonly Lazy<ScanResult> Repository = new(() =>
        new PersonNumberScanner(OfficialNumbers.All).Scan(RepositoryTexts.Under(RepositoryRoot.Locate())));

    [Test]
    public async Task EveryPersonShapedNumber_IsOfficial()
    {
        var unofficial = Repository.Value.Unofficial;
        await Assert.That(unofficial.Count).IsEqualTo(0).Because(string.Join(Environment.NewLine, unofficial));
    }

    [Test]
    public async Task Scan_ReachesThisSourceFileAndEveryOfficialRow()
    {
        var scanned = RepositoryTexts.Under(RepositoryRoot.Locate()).Select(text => text.RelativePath).ToList();
        await Assert.That(scanned.Any(path => Path.GetFileName(path) == $"{nameof(RepositoryNumbersTests)}.cs")).IsTrue();
        await Assert.That(scanned.Any(path => Path.GetFileName(path) == "README.md")).IsTrue();
        await Assert.That(Repository.Value.FilesScanned).IsEqualTo(scanned.Count);
        await Assert.That(Repository.Value.OfficialHits).IsGreaterThanOrEqualTo(OfficialNumbers.All.Count());
    }

    [Test]
    public async Task Scan_LeavesOutBuildOutput()
    {
        var parts = RepositoryTexts.Under(RepositoryRoot.Locate())
            .SelectMany(text => text.RelativePath.Split(Path.DirectorySeparatorChar))
            .ToHashSet();
        await Assert.That(parts.Contains("obj")).IsFalse();
        await Assert.That(parts.Contains(".git")).IsFalse();
    }
}
