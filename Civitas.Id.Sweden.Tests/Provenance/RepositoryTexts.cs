using System.Text;

namespace Civitas.Id.Sweden.Tests.Provenance;

/// <summary>Reads the text files under a directory that its ignore rules do not leave out.</summary>
internal static class RepositoryTexts
{
    private const string IgnoreFile = ".gitignore";

    // Git's own directory, and the local tool directory that Git excludes outside .gitignore.
    private static readonly string[] AlwaysIgnored = [".git", ".superpowers"];

    /// <summary>Every text file under <paramref name="root" />, with paths relative to it.</summary>
    public static IEnumerable<SourceText> Under(DirectoryInfo root)
    {
        var rules = IgnoreRules.Parse(File.ReadLines(Path.Combine(root.FullName, IgnoreFile)), AlwaysIgnored);
        return Files(root, rules)
            .Select(file => (file, bytes: File.ReadAllBytes(file.FullName)))
            .Where(read => IsText(read.bytes))
            .Select(read => new SourceText(
                Path.GetRelativePath(root.FullName, read.file.FullName), Encoding.UTF8.GetString(read.bytes)));
    }

    private static IEnumerable<FileInfo> Files(DirectoryInfo directory, IgnoreRules rules)
    {
        // In a linked worktree .git is a file, so it is left out by name here too.
        var here = directory.EnumerateFiles()
            .Where(file => !rules.IgnoresFile(file.Name) && !rules.IgnoresDirectory(file.Name));
        var below = directory.EnumerateDirectories()
            .Where(child => !rules.IgnoresDirectory(child.Name))
            .SelectMany(child => Files(child, rules));
        return here.Concat(below);
    }

    // Git's own test: a file with a zero byte is binary.
    private static bool IsText(byte[] bytes)
    {
        return !bytes.Contains((byte)0);
    }
}
