// Linked into several test projects, each of which uses only some of the members.
// ReSharper disable UnusedMember.Global

using System.Security.Cryptography;

namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>The one home for where fixtures live, how their content is hashed, and the missing-file spec.</summary>
internal static class FixtureFiles
{
    /// <summary>The output directory the csproj copies <c>Fixtures/</c> into.</summary>
    public static readonly string Directory = Path.Combine(AppContext.BaseDirectory, "Fixtures");

    /// <summary>A spec naming a file that does not exist, for fail-closed tests.</summary>
    public static readonly FixtureSpec Missing = new()
    {
        FileName = "does-not-exist.csv",
        DataRows = 1,
        Sha256 = Sha256Hex([]),
        HasHeader = false
    };

    /// <summary>Lower-case hex SHA-256, the form <see cref="FixtureSpec.Sha256" /> is pinned in.</summary>
    public static string Sha256Hex(ReadOnlySpan<byte> bytes)
    {
        return Convert.ToHexStringLower(SHA256.HashData(bytes));
    }
}
