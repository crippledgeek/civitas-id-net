using System.Text;

namespace Civitas.Id.Sweden.Tests.Provenance;

/// <summary>Reads the text files of the repository: those Git tracks, and new ones that it does not ignore.</summary>
internal static class RepositoryTexts
{
    // Git calls a file binary when a zero byte is among its first 8000 bytes.
    private const int BinaryProbeLength = 8000;

    // Longest byte order mark first, because UTF-32's begins with UTF-16's.
    private static readonly Encoding[] MarkedEncodings =
        [Encoding.UTF32, new UTF32Encoding(true, true), Encoding.UTF8, Encoding.Unicode, Encoding.BigEndianUnicode];

    /// <summary>Every text file under <paramref name="root" />, the root of a Git working tree.</summary>
    /// <remarks>
    ///     A file that Git lists and the working tree lacks must be one Git calls deleted. Any other, such as one left
    ///     out of a sparse checkout, throws, because the commit may hold it and it cannot be read here.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Git fails or lists no file, or a file is missing.</exception>
    /// <exception cref="TimeoutException">Git runs for too long.</exception>
    public static IReadOnlyList<SourceText> Under(DirectoryInfo root)
    {
        return Under(root, GitEnvironment.Current);
    }

    /// <inheritdoc cref="Under(DirectoryInfo)" />
    public static IReadOnlyList<SourceText> Under(DirectoryInfo root, GitEnvironment environment)
    {
        var git = new GitProcess(root, environment);
        var listed = Paths(git, "--cached", "--others", "--exclude-standard");
        if (listed.Count == 0)
            throw new InvalidOperationException("Git lists no files.");

        var deleted = Paths(git, "--deleted");
        return [.. listed.Except(deleted).Select(path => Read(root, path)).OfType<SourceText>()];
    }

    private static HashSet<RelativePath> Paths(GitProcess git, params string[] selection)
    {
        return
        [
            .. git.Run(["ls-files", "-z", .. selection])
                .Split('\0', StringSplitOptions.RemoveEmptyEntries)
                .Select(path => new RelativePath(path))
        ];
    }

    // Null for a binary file and for a directory, which Git lists only when it is another repository.
    private static SourceText? Read(DirectoryInfo root, RelativePath path)
    {
        var file = new FileInfo(Path.Combine(root.FullName, path.Value));
        if (file.LinkTarget is { } target)
            return new SourceText(path, target);
        if (Directory.Exists(file.FullName))
            return null;
        if (!file.Exists)
            throw new InvalidOperationException($"Git lists {path}, which is not in the working tree.");

        var bytes = File.ReadAllBytes(file.FullName);
        return Decode(bytes) is { } content ? new SourceText(path, content) : null;
    }

    private static string? Decode(byte[] bytes)
    {
        var marked = MarkedEncodings.FirstOrDefault(encoding => bytes.AsSpan().StartsWith(encoding.Preamble));
        if (marked is not null)
            return marked.GetString(bytes.AsSpan(marked.Preamble.Length));

        return bytes.AsSpan(0, Math.Min(bytes.Length, BinaryProbeLength)).Contains((byte)0)
            ? null
            : Encoding.UTF8.GetString(bytes);
    }
}
