using System.Text.RegularExpressions;

namespace Civitas.Id.Sweden.AspNetCore.Tests.Architecture;

/// <summary>
/// Syntactic source-text invariants for the AspNetCore package. Complements
/// <see cref="AuditRemediationArchRules"/> with checks that operate on the raw
/// .cs files rather than the compiled IL (ArchUnitNET cannot observe a C# 14
/// extension(T) block — it desugars to a sealed static class indistinguishable
/// from a classic <c>this T</c> extension at IL level).
/// </summary>
public sealed partial class SourceSyntaxRules
{
    // Match the C# 14 `extension(...)` block-declaration form only — not method calls.
    [GeneratedRegex(@"^\s*extension\s*\(", RegexOptions.Multiline)]
    private static partial Regex ExtensionBlockRegex();

    [Test]
    public async Task No_Csharp14_ExtensionBlock_In_AspNetCore_Source()
    {
        // AppContext.BaseDirectory points at the test bin/ output during test execution;
        // walk up to the repo root and reach the AspNetCore source tree from there.
        var sourceRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "Civitas.Id.Sweden.AspNetCore"));

        await Assert.That(Directory.Exists(sourceRoot)).IsTrue()
            .Because($"AspNetCore source tree must be reachable from test bin/; computed: {sourceRoot}");

        var sourceFiles = Directory.GetFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToArray();

        await Assert.That(sourceFiles.Length).IsGreaterThan(0)
            .Because("at least one source file must exist under the AspNetCore package");

        foreach (var file in sourceFiles)
        {
            var content = await File.ReadAllTextAsync(file);
            var hasExtBlock = ExtensionBlockRegex().IsMatch(content);
            await Assert.That(hasExtBlock).IsFalse()
                .Because($"File {file} introduces a C# 14 extension(T) block — reverted to classic 'this T' per Finding 8.");
        }
    }
}
