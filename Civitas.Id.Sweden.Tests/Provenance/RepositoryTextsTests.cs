using System.Diagnostics;
using System.Text;
using Civitas.Id.Sweden.Tests.Helpers;

namespace Civitas.Id.Sweden.Tests.Provenance;

public class RepositoryTextsTests
{
    private const string Content = "some text";

    [Test]
    public async Task NotARepository_Throws()
    {
        using var directory = new TemporaryDirectory();
        var root = directory.Root;
        var ex = await Assert.That(() => RepositoryTexts.Under(root)).ThrowsExactly<InvalidOperationException>();
        await Assert.That(ex!.Message).Contains("exit code");
    }

    [Test]
    public async Task EmptyRepository_Throws()
    {
        using var directory = TemporaryDirectory.WithRepository();
        var root = directory.Root;
        await Assert.That(() => RepositoryTexts.Under(root)).ThrowsExactly<InvalidOperationException>();
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
    public async Task SymbolicLink_IsItsTargetText_AndIsNotFollowed()
    {
        using var outside = new TemporaryDirectory();
        outside.Write("outside.txt", Content);
        using var directory = TemporaryDirectory.WithRepository();
        directory.Link("link", outside.Root.FullName);
        directory.Link("dangling", "no-such-target");

        var texts = RepositoryTexts.Under(directory.Root).ToDictionary(text => text.Path.Value, text => text.Content);
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

        await Assert.That(RepositoryTexts.Under(directory.Root).Single().Content).IsEqualTo(Content);
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

        await Assert.That(RepositoryTexts.Under(directory.Root).Count == 1).IsEqualTo(isText);
    }

    private static HashSet<string> Paths(TemporaryDirectory directory)
    {
        return [.. RepositoryTexts.Under(directory.Root).Select(text => text.Path.Value)];
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
            using var git = Process.Start(new ProcessStartInfo("git", arguments) { WorkingDirectory = Root.FullName })!;
            git.WaitForExit();
            if (git.ExitCode != 0)
                throw new InvalidOperationException($"Git failed: exit code {git.ExitCode}.");
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
