using System.Security.Cryptography;

namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>The one home for where fixtures live and how their content is hashed.</summary>
internal static class FixtureFiles
{
    /// <summary>The output directory the csproj copies <c>Fixtures/</c> into.</summary>
    public static readonly string Directory = Path.Combine(AppContext.BaseDirectory, "Fixtures");

    /// <summary>Lower-case hex SHA-256, the form <see cref="FixtureSpec.Sha256" /> is pinned in.</summary>
    public static string Sha256Hex(ReadOnlySpan<byte> bytes)
    {
        return Convert.ToHexStringLower(SHA256.HashData(bytes));
    }
}
