using System.Diagnostics;
using System.Text;

namespace Civitas.Id.Sweden.Tests.Provenance;

/// <summary>Reads the text files of the repository: those Git tracks, and new ones that it does not ignore.</summary>
internal static class RepositoryTexts
{
    // Git calls a file binary when a zero byte is among its first 8000 bytes.
    private const int BinaryProbeLength = 8000;

    private static readonly string[] ListFiles =
        ["ls-files", "-z", "--cached", "--others", "--exclude-standard"];

    // Longest byte order mark first, because UTF-32's begins with UTF-16's.
    private static readonly Encoding[] MarkedEncodings =
        [Encoding.UTF32, new UTF32Encoding(true, true), Encoding.UTF8, Encoding.Unicode, Encoding.BigEndianUnicode];

    /// <summary>Every text file under <paramref name="root" />, the root of a Git working tree.</summary>
    /// <exception cref="InvalidOperationException">Git fails or lists no file.</exception>
    public static IReadOnlyList<SourceText> Under(DirectoryInfo root)
    {
        return [.. Paths(root).Select(path => Read(root, path)).OfType<SourceText>()];
    }

    private static List<RelativePath> Paths(DirectoryInfo root)
    {
        var start = new ProcessStartInfo("git", ListFiles)
        {
            WorkingDirectory = root.FullName,
            RedirectStandardOutput = true,
            StandardOutputEncoding = new UTF8Encoding(false)
        };
        using var git = Process.Start(start) ?? throw new InvalidOperationException("Git did not start.");
        var output = git.StandardOutput.ReadToEnd();
        git.WaitForExit();
        if (git.ExitCode != 0)
            throw new InvalidOperationException($"Git could not list the files: exit code {git.ExitCode}.");

        var paths = output.Split('\0', StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.Ordinal)
            .Select(path => new RelativePath(path))
            .ToList();
        return paths.Count > 0 ? paths : throw new InvalidOperationException("Git lists no files.");
    }

    // Null for a binary file, a file deleted from the working tree, and a nested repository.
    private static SourceText? Read(DirectoryInfo root, RelativePath path)
    {
        var file = new FileInfo(Path.Combine(root.FullName, path.Value));
        if (file.LinkTarget is { } target)
            return new SourceText(path, target);
        if (!file.Exists)
            return null;

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
