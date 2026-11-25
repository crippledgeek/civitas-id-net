using Civitas.Id.Sweden.Tests.Helpers;

namespace Civitas.Id.Sweden.Tests.Provenance;

/// <summary>A person-shaped number in the repository must be in a pinned official test set.</summary>
public class RepositoryNumbersTests
{
    private const string TestProject = "Civitas.Id.Sweden.Tests";

    private static readonly Lazy<ScanResult> Repository = new(() =>
        new PersonNumberScanner(OfficialNumbers.All).Scan(RepositoryTexts.Under(RepositoryRoot.Locate())));

    private static FileScan FileAt(string path)
    {
        return Repository.Value.Files.Single(file => file.Path.Value == path);
    }

    [Test]
    public async Task EveryPersonShapedNumber_IsOfficial()
    {
        var unofficial = Repository.Value.Unofficial;
        await Assert.That(unofficial.Count).IsEqualTo(0).Because(string.Join(Environment.NewLine, unofficial));
    }

    /// <summary>The files of one published or copied number a row.</summary>
    public static IEnumerable<FixtureSpec> NumberFiles()
    {
        return
        [
            Fixtures.Personnummer1890S, Fixtures.Personnummer2000S, Fixtures.PersonnummerFinal,
            Fixtures.SamordningsnummerOfficial, Fixtures.Samordningsnummer2020, Fixtures.SamordningsnummerCorrected
        ];
    }

    [Test]
    [MethodDataSource(nameof(NumberFiles))]
    public async Task NumberFile_HasOneOfficialNumberARow(FixtureSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var file = FileAt($"{TestProject}/Fixtures/{spec.FileName}");
        await Assert.That(file.OfficialHits).IsEqualTo(spec.DataRows);
    }

    [Test]
    [Arguments("README.md")]
    [Arguments("CHANGELOG.md")]
    [Arguments(".github/workflows/ci.yml")]
    [Arguments("Civitas.Id.Sweden/README.md")]
    [Arguments("Civitas.Id.Sweden/Core/PersonalId.cs")]
    [Arguments($"{TestProject}/Provenance/{nameof(RepositoryNumbersTests)}.cs")]
    public async Task File_IsScanned(string path)
    {
        await Assert.That(FileAt(path).Path.Value).IsEqualTo(path);
    }

    [Test]
    [Arguments("bin")]
    [Arguments("obj")]
    [Arguments(".git")]
    [Arguments(".nuget")]
    public async Task Directory_IsLeftOut(string name)
    {
        var inside = Repository.Value.Files.Count(file => file.Path.Value.Split('/').SkipLast(1).Contains(name));
        await Assert.That(inside).IsEqualTo(0);
    }
}
