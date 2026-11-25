using System.Text;
using Civitas.Id.Sweden.Tests.Helpers;

namespace Civitas.Id.Sweden.Tests.Provenance;

public class RepositoryTextsTests
{
    private const string Content = "some text";

    private static readonly GitEnvironment Fixture = GitEnvironment.Current.WithoutConfiguration();

    [Test]
    public async Task NotARepository_Throws()
    {
        using var directory = new TemporaryDirectory();
        var root = directory.Root;
        var ex = await Assert.That(() => RepositoryTexts.Under(root, Fixture)).ThrowsExactly<InvalidOperationException>();
        await Assert.That(ex!.Message).Contains("exit code");
    }

    [Test]
    public async Task EmptyRepository_Throws()
    {
        using var directory = TemporaryDirectory.WithRepository();
        var root = directory.Root;
        await Assert.That(() => RepositoryTexts.Under(root, Fixture)).ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task TrackedAndNewFiles_AreListed_IgnoredOnesAreNot()
    {
        using var directory = TemporaryDirectory.WithRepository();
        directory.Write(".gitignore", "docs/\nignored.txt\n");
        directory.Write("ignored.txt", Content);
        directory.Write("docs/ignored.txt", Content);
        directory.Write("tracked.txt", Content);
        directory.Git("add", "tracked.txt");
        directory.Git("add", "--force", "ignored.txt");
        directory.Write("new.txt", Content);

        await Assert.That(Paths(directory).SetEquals([".gitignore", "ignored.txt", "tracked.txt", "new.txt"])).IsTrue();
    }

    [Test]
    public async Task PackageCache_IsIgnoredByThisRepositorysRules()
    {
        using var directory = TemporaryDirectory.WithRepository();
        var rules = Path.Combine(RepositoryRoot.Locate().FullName, ".gitignore");
        directory.Write(".gitignore", await File.ReadAllTextAsync(rules).ConfigureAwait(false));
        directory.Write(".nuget/packages/a/readme.md", Content);

        await Assert.That(Paths(directory).SetEquals([".gitignore"])).IsTrue();
    }

    [Test]
    public async Task FileNamedLikeAnIgnoredDirectory_IsListed()
    {
        using var directory = TemporaryDirectory.WithRepository();
        directory.Write(".gitignore", "docs/\n");
        directory.Write("docs", Content);

        await Assert.That(Paths(directory).SetEquals([".gitignore", "docs"])).IsTrue();
    }

    [Test]
    public async Task TrackedFileMissingOnDisk_IsLeftOut()
    {
        using var directory = TemporaryDirectory.WithRepository();
        directory.Write("kept.txt", Content);
        directory.Write("deleted.txt", Content);
        directory.Git("add", "kept.txt", "deleted.txt");
        directory.Delete("deleted.txt");

        await Assert.That(Paths(directory).SetEquals(["kept.txt"])).IsTrue();
    }

    [Test]
    public async Task TrackedFileOutsideASparseCheckout_Throws()
    {
        using var directory = TemporaryDirectory.WithRepository();
        directory.Write("kept.txt", Content);
        directory.Write("sparse.txt", Content);
        directory.Git("add", "kept.txt", "sparse.txt");
        directory.Git("update-index", "--skip-worktree", "sparse.txt");
        directory.Delete("sparse.txt");
        var root = directory.Root;

        var ex = await Assert.That(() => RepositoryTexts.Under(root, Fixture)).ThrowsExactly<InvalidOperationException>();
        await Assert.That(ex!.Message).Contains("sparse.txt");
    }

    [Test]
    public async Task NestedRepository_IsLeftOut()
    {
        using var directory = TemporaryDirectory.WithRepository();
        directory.Write("kept.txt", Content);
        directory.Write("nested/inside.txt", Content);
        directory.Git("init", "--quiet", "nested");

        await Assert.That(Paths(directory).SetEquals(["kept.txt"])).IsTrue();
    }

    /// <summary>Variables that point Git at another repository.</summary>
    public static IEnumerable<(string Name, string Below)> HostileVariables()
    {
        yield return ("GIT_DIR", ".git");
        yield return ("GIT_WORK_TREE", "");
        yield return ("GIT_INDEX_FILE", ".git/index");
        yield return ("GIT_COMMON_DIR", ".git");
        yield return ("GIT_OBJECT_DIRECTORY", ".git/objects");
        yield return ("git_dir", ".git");
    }

    [Test]
    [MethodDataSource(nameof(HostileVariables))]
    public async Task InheritedVariable_PointsGitNowhereElse(string name, string below)
    {
        using var other = TemporaryDirectory.WithRepository();
        other.Write("other.txt", Content);
        other.Git("add", "other.txt");
        using var directory = TemporaryDirectory.WithRepository();
        directory.Write("own.txt", Content);
        var hostile = Fixture.Inherit(name, Path.Combine(other.Root.FullName, below));

        new GitProcess(directory.Root, hostile).Run("add", "own.txt");
        var texts = RepositoryTexts.Under(directory.Root, hostile);

        await Assert.That(texts.Single().Path.Value).IsEqualTo("own.txt");
        await Assert.That(new GitProcess(directory.Root, Fixture).Run("ls-files")).IsEqualTo("own.txt\n");
        await Assert.That(new GitProcess(other.Root, Fixture).Run("ls-files")).IsEqualTo("other.txt\n");
    }

    [Test]
    public async Task SymbolicLink_IsItsTargetText_AndIsNotFollowed()
    {
        Skip.When(OperatingSystem.IsWindows(), "Creating a symbolic link needs a privilege on Windows.");
        using var outside = new TemporaryDirectory();
        outside.Write("outside.txt", Content);
        using var directory = TemporaryDirectory.WithRepository();
        directory.Link("link", outside.Root.FullName);
        directory.Link("dangling", "no-such-target");

        var texts = RepositoryTexts.Under(directory.Root, Fixture).ToDictionary(text => text.Path.Value, text => text.Content);
        await Assert.That(texts.Count).IsEqualTo(2);
        await Assert.That(texts["link"]).IsEqualTo(outside.Root.FullName);
        await Assert.That(texts["dangling"]).IsEqualTo("no-such-target");
    }

    /// <summary>Encodings whose byte order mark makes a file text even with zero bytes in it.</summary>
    public static IEnumerable<Func<Encoding>> MarkedEncodings()
    {
        yield return () => Encoding.UTF8;
        yield return () => Encoding.Unicode;
        yield return () => Encoding.BigEndianUnicode;
        yield return () => Encoding.UTF32;
        yield return () => new UTF32Encoding(true, true);
    }

    [Test]
    [MethodDataSource(nameof(MarkedEncodings))]
    public async Task MarkedFile_IsDecoded(Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(encoding);
        using var directory = TemporaryDirectory.WithRepository();
        directory.Write("a.txt", [.. encoding.GetPreamble(), .. encoding.GetBytes(Content)]);

        await Assert.That(RepositoryTexts.Under(directory.Root, Fixture).Single().Content).IsEqualTo(Content);
    }

    [Test]
    [Arguments(7999, false)]
    [Arguments(8000, true)]
    public async Task ZeroByte_MakesAFileBinaryInItsFirst8000Bytes(int position, bool isText)
    {
        using var directory = TemporaryDirectory.WithRepository();
        var bytes = Enumerable.Repeat((byte)'a', position + 1).ToArray();
        bytes[position] = 0;
        directory.Write("a.bin", bytes);

        await Assert.That(RepositoryTexts.Under(directory.Root, Fixture).Count == 1).IsEqualTo(isText);
    }

    private static HashSet<string> Paths(TemporaryDirectory directory)
    {
        return [.. RepositoryTexts.Under(directory.Root, Fixture).Select(text => text.Path.Value)];
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public DirectoryInfo Root { get; } = Directory.CreateTempSubdirectory("civitas-texts-");

        public static TemporaryDirectory WithRepository()
        {
            var directory = new TemporaryDirectory();
            directory.Git("init", "--quiet");
            return directory;
        }

        public void Write(string name, string content)
        {
            Write(name, Encoding.ASCII.GetBytes(content));
        }

        public void Write(string name, byte[] bytes)
        {
            var path = Path.Combine(Root.FullName, name);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, bytes);
        }

        public void Link(string name, string target)
        {
            File.CreateSymbolicLink(Path.Combine(Root.FullName, name), target);
        }

        public void Delete(string name)
        {
            File.Delete(Path.Combine(Root.FullName, name));
        }

        public void Git(params string[] arguments)
        {
            new GitProcess(Root, Fixture).Run(arguments);
        }

        public void Dispose()
        {
            // Git writes its objects read-only.
            foreach (var file in Root.EnumerateFiles("*", SearchOption.AllDirectories).Where(f => f.LinkTarget is null))
                file.IsReadOnly = false;
            Root.Delete(true);
        }
    }
}
