using System.Collections;

namespace Civitas.Id.Sweden.Tests.Provenance;

/// <summary>
///     The environment of a Git child process: an inherited one without any <c>GIT_*</c> variable, which could point
///     Git at another repository, and then the variables added on purpose. Names and values are
///     <see cref="string" /> because the operating system puts no other rule on them.
/// </summary>
internal sealed record GitEnvironment
{
    private const string GitPrefix = "GIT_";

    private GitEnvironment(IReadOnlyDictionary<string, string> inherited, IReadOnlyDictionary<string, string> added)
    {
        Inherited = inherited;
        Added = added;
    }

    private IReadOnlyDictionary<string, string> Inherited { get; }

    private IReadOnlyDictionary<string, string> Added { get; }

    /// <summary>The environment of this process.</summary>
    public static GitEnvironment Current { get; } = Inheriting(
        Environment.GetEnvironmentVariables().Cast<DictionaryEntry>()
            .ToDictionary(entry => (string)entry.Key, entry => (string?)entry.Value ?? string.Empty));

    /// <summary>An environment that inherits <paramref name="variables" /> and takes no optional locks.</summary>
    public static GitEnvironment Inheriting(IReadOnlyDictionary<string, string> variables)
    {
        return new GitEnvironment(variables, new Dictionary<string, string> { ["GIT_OPTIONAL_LOCKS"] = "0" });
    }

    /// <summary>This environment with more inherited variables, as a parent process would set them.</summary>
    public GitEnvironment Inherit(string name, string value)
    {
        return new GitEnvironment(new Dictionary<string, string>(Inherited) { [name] = value }, Added);
    }

    /// <summary>This environment without the user's and the system's Git configuration.</summary>
    public GitEnvironment WithoutConfiguration()
    {
        var nothing = OperatingSystem.IsWindows() ? "NUL" : "/dev/null";
        return new GitEnvironment(Inherited, new Dictionary<string, string>(Added)
        {
            ["GIT_CONFIG_GLOBAL"] = nothing,
            ["GIT_CONFIG_SYSTEM"] = nothing
        });
    }

    /// <summary>The variables to start Git with.</summary>
    public IEnumerable<KeyValuePair<string, string>> Variables()
    {
        return Inherited
            .Where(variable => !variable.Key.StartsWith(GitPrefix, StringComparison.OrdinalIgnoreCase))
            .Concat(Added);
    }
}
