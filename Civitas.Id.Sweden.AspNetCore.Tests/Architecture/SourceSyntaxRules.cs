using System.Runtime.CompilerServices;
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
        var sourceRoot = LocateAspNetCoreSourceRoot();

        await Assert.That(Directory.Exists(sourceRoot)).IsTrue()
            .Because($"AspNetCore source tree must be reachable from the test file's own location; computed: {sourceRoot}");

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

    /// <summary>
    /// Resolves the absolute path to the Civitas.Id.Sweden.AspNetCore source
    /// tree by anchoring on <see cref="CallerFilePathAttribute"/>, which the
    /// compiler resolves to the absolute path of THIS source file at compile
    /// time. This is robust against MSBuild output-directory changes,
    /// alternative build hosts, and `dotnet test` vs `dotnet run` invocation
    /// differences — unlike anchoring on <see cref="AppContext.BaseDirectory"/>.
    /// </summary>
    private static string LocateAspNetCoreSourceRoot([CallerFilePath] string callerFilePath = "")
    {
        // callerFilePath:
        //   <repo>/Civitas.Id.Sweden.AspNetCore.Tests/Architecture/SourceSyntaxRules.cs
        // Walk up to the test-project directory, then to repo root, then sibling-jump.
        var architectureDir = Path.GetDirectoryName(callerFilePath)!;          // .../Architecture
        var testProjectDir = Path.GetDirectoryName(architectureDir)!;          // .../Civitas.Id.Sweden.AspNetCore.Tests
        var repoRoot = Path.GetDirectoryName(testProjectDir)!;                 // <repo>
        var libRoot = Path.Combine(repoRoot, "Civitas.Id.Sweden.AspNetCore");

        if (!Directory.Exists(libRoot))
        {
            throw new InvalidOperationException(
                $"Could not locate Civitas.Id.Sweden.AspNetCore source. "
                + $"Computed path: '{libRoot}'. "
                + $"CallerFilePath: '{callerFilePath}'.");
        }

        return libRoot;
    }
}
