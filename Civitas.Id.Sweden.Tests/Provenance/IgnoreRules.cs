using System.IO.Enumeration;

namespace Civitas.Id.Sweden.Tests.Provenance;

/// <summary>
///     The repository's ignore rules, as far as its <c>.gitignore</c> uses the syntax: directory names ending in
///     <c>/</c>, and file names with <c>*</c>. Any other syntax throws, so that a rule is never silently misread.
/// </summary>
internal sealed class IgnoreRules
{
    private const char DirectoryMark = '/';

    private readonly HashSet<string> _directories;
    private readonly string[] _filePatterns;

    private IgnoreRules(HashSet<string> directories, string[] filePatterns)
    {
        _directories = directories;
        _filePatterns = filePatterns;
    }

    /// <summary>Reads the rules, adding directories that Git itself or a local exclude file leaves out.</summary>
    public static IgnoreRules Parse(IEnumerable<string> lines, params string[] alwaysIgnoredDirectories)
    {
        var rules = lines.Select(line => line.Trim()).Where(line => line.Length > 0 && line[0] != '#').ToList();
        if (rules.FirstOrDefault(UsesOtherSyntax) is { } unsupported)
            throw new NotSupportedException($"Ignore rule '{unsupported}' uses syntax this reader does not know.");

        var byKind = rules.ToLookup(rule => rule[^1] == DirectoryMark);
        return new IgnoreRules(
            [.. byKind[true].Select(rule => rule.TrimEnd(DirectoryMark)), .. alwaysIgnoredDirectories],
            [.. byKind[false]]);
    }

    /// <summary>True for a directory name the rules leave out.</summary>
    public bool IgnoresDirectory(string name)
    {
        return _directories.Contains(name);
    }

    /// <summary>True for a file name the rules leave out.</summary>
    public bool IgnoresFile(string name)
    {
        return _filePatterns.Any(pattern => FileSystemName.MatchesSimpleExpression(pattern, name, ignoreCase: false));
    }

    private static bool UsesOtherSyntax(string rule)
    {
        var directoryWithWildcard = rule[^1] == DirectoryMark && rule.Contains('*', StringComparison.Ordinal);
        var path = rule.TrimEnd(DirectoryMark).Contains(DirectoryMark, StringComparison.Ordinal);
        return path || directoryWithWildcard || rule.AsSpan().ContainsAny("!?[\\");
    }
}
