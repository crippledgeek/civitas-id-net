// Linked into several test projects, each of which uses only some of the members.
// ReSharper disable UnusedType.Global, UnusedMember.Global

using System.Runtime.CompilerServices;

namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>Finds the repository root from the compile-time path of this source file.</summary>
internal static class RepositoryRoot
{
    private const string SolutionFile = "CivitasId.slnx";

    /// <summary>The root directory, the nearest one above this file that holds the solution file.</summary>
    /// <exception cref="InvalidOperationException">No directory above this file holds the solution file.</exception>
    public static DirectoryInfo Locate()
    {
        var source = ThisFile();
        for (var directory = Directory.GetParent(source); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, SolutionFile)))
                return directory;

        throw new InvalidOperationException($"No directory above '{source}' holds {SolutionFile}.");
    }

    // The compiler fills in the absolute path of this file, so the result does not depend on the output
    // directory or on how the tests are started.
    private static string ThisFile([CallerFilePath] string path = "")
    {
        return path;
    }
}
