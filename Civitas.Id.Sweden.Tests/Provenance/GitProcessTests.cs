namespace Civitas.Id.Sweden.Tests.Provenance;

public class GitProcessTests
{
    private static GitProcess InTemporaryDirectory()
    {
        return new GitProcess(new DirectoryInfo(Path.GetTempPath()), GitEnvironment.Current.WithoutConfiguration());
    }

    [Test]
    public async Task Failure_CarriesTheExitCodeAndGitsOwnMessage()
    {
        var ex = await Assert.That(() => InTemporaryDirectory().Run("no-such-command"))
            .ThrowsExactly<InvalidOperationException>();
        await Assert.That(ex!.Message).Contains("exit code 1");
        await Assert.That(ex.Message).Contains("no-such-command");
    }

    [Test]
    public async Task NotStarted_Throws()
    {
        var absent = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "no-such-directory"));
        var git = new GitProcess(absent, GitEnvironment.Current);
        await Assert.That(() => git.Run("version")).ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task TooSlow_IsKilled()
    {
        var git = InTemporaryDirectory() with { Timeout = TimeSpan.FromMilliseconds(200) };
        await Assert.That(() => git.Run("-c", "alias.wait=!sleep 30", "wait")).ThrowsExactly<TimeoutException>();
    }

    [Test]
    public async Task InheritedGitVariables_AreRemoved_AndOthersKept()
    {
        var names = GitEnvironment.Inheriting(new Dictionary<string, string> { ["GIT_DIR"] = "x", ["git_x"] = "x", ["A"] = "x" })
            .WithoutConfiguration().Variables().Select(variable => variable.Key).ToHashSet();
        await Assert.That(names.SetEquals(["A", "GIT_OPTIONAL_LOCKS", "GIT_CONFIG_GLOBAL", "GIT_CONFIG_SYSTEM"])).IsTrue();
    }
}
