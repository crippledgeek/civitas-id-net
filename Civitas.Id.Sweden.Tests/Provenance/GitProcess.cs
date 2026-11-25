using System.Diagnostics;
using System.Text;

namespace Civitas.Id.Sweden.Tests.Provenance;

/// <summary>Runs Git, found through the search path, in one directory.</summary>
internal sealed record GitProcess(DirectoryInfo Directory, GitEnvironment Environment)
{
    /// <summary>How long Git may run before it is killed.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(1);

    /// <summary>Runs Git with a list of arguments, without a shell, and returns its output.</summary>
    /// <exception cref="InvalidOperationException">Git does not start, or ends with another exit code than zero.</exception>
    /// <exception cref="TimeoutException">Git runs for longer than <see cref="Timeout" />.</exception>
    public string Run(params string[] arguments)
    {
        using var git = Process.Start(StartInfo(arguments))
                        ?? throw new InvalidOperationException("Git did not start.");
        git.StandardInput.Close();

        // Both are read while Git runs, so that a full pipe cannot stop it.
        var output = git.StandardOutput.ReadToEndAsync();
        var errors = git.StandardError.ReadToEndAsync();
        if (!git.WaitForExit(Timeout))
        {
            git.Kill(true);
            throw new TimeoutException($"Git ran for longer than {Timeout}.");
        }

        if (git.ExitCode != 0)
            throw new InvalidOperationException(
                $"Git failed with exit code {git.ExitCode}: {errors.GetAwaiter().GetResult().Trim()}");
        return output.GetAwaiter().GetResult();
    }

    private ProcessStartInfo StartInfo(string[] arguments)
    {
        var start = new ProcessStartInfo("git", arguments)
        {
            WorkingDirectory = Directory.FullName,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = new UTF8Encoding(false)
        };
        start.Environment.Clear();
        foreach (var (name, value) in Environment.Variables())
            start.Environment[name] = value;
        return start;
    }
}
