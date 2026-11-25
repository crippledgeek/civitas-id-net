namespace Civitas.Id.Sweden.Tests.Provenance;

public class IgnoreRulesTests
{
    private static readonly IgnoreRules Rules =
        IgnoreRules.Parse(["# comment", "", "bin/", "*.received.*", "CLAUDE.md"], ".git");

    [Test]
    [Arguments("bin", true)]
    [Arguments(".git", true)]
    [Arguments("binary", false)]
    [Arguments("CLAUDE.md", false)]
    public async Task IgnoresDirectory_ByExactName(string name, bool expected)
    {
        await Assert.That(Rules.IgnoresDirectory(name)).IsEqualTo(expected);
    }

    [Test]
    [Arguments("x.received.txt", true)]
    [Arguments("CLAUDE.md", true)]
    [Arguments("x.verified.txt", false)]
    [Arguments("bin", false)]
    public async Task IgnoresFile_ByPattern(string name, bool expected)
    {
        await Assert.That(Rules.IgnoresFile(name)).IsEqualTo(expected);
    }

    [Test]
    [Arguments("!kept.txt")]
    [Arguments("/rooted/")]
    [Arguments("a/b")]
    [Arguments("b*/")]
    [Arguments("[ab].txt")]
    public async Task OtherSyntax_Throws(string rule)
    {
        await Assert.That(() => IgnoreRules.Parse([rule])).Throws<NotSupportedException>();
    }
}
