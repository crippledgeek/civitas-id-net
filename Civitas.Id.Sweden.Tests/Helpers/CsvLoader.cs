// Linked into several test projects, each of which uses only some of the members.
// ReSharper disable MemberCanBePrivate.Global

namespace Civitas.Id.Sweden.Tests.Helpers;

internal static class CsvLoader
{
    /// <summary>
    ///     Loads the fixture's data rows (header excluded). Fails closed: a missing file throws
    ///     <see cref="FileNotFoundException" />; a content hash that differs from the spec, no data rows,
    ///     or a row count that differs from the spec throws <see cref="InvalidDataException" />.
    /// </summary>
    public static IReadOnlyList<FixtureRow> Rows(FixtureSpec spec)
    {
        return Rows(spec, FixtureFiles.Directory);
    }

    /// <inheritdoc cref="Rows(FixtureSpec)" />
    public static IReadOnlyList<FixtureRow> Rows(FixtureSpec spec, string directory)
    {
        var path = Path.Combine(directory, spec.FileName);
        var bytes = File.ReadAllBytes(path);

        var actualHash = FixtureFiles.Sha256Hex(bytes);
        if (actualHash != spec.Sha256)
            throw new InvalidDataException(
                $"{spec.FileName}: SHA-256 is {actualHash}, expected {spec.Sha256}.");

        var rows = ParseLines(spec, System.Text.Encoding.UTF8.GetString(bytes));
        if (rows.Count == 0)
            throw new InvalidDataException($"{spec.FileName}: no data rows.");
        if (rows.Count != spec.DataRows)
            throw new InvalidDataException(
                $"{spec.FileName}: {rows.Count} data rows, expected {spec.DataRows}.");

        return rows;
    }

    /// <summary>The cell at <paramref name="index" /> (default: the first) of every data row of a verified fixture.
    /// The index is a plain <see cref="int" />: a position in a test file, with no domain meaning or invariant.</summary>
    public static IReadOnlyList<string> Column(FixtureSpec spec, int index = 0)
    {
        return [.. Rows(spec).Select(row => row.Cells[index])];
    }

    private static List<FixtureRow> ParseLines(FixtureSpec spec, string text)
    {
        var rows = new List<FixtureRow>();
        var headerPending = spec.HasHeader;
        var line = 0;
        using var reader = new StringReader(text);
        while (reader.ReadLine() is { } content)
        {
            line++;
            if (string.IsNullOrWhiteSpace(content))
                continue;
            if (headerPending)
            {
                headerPending = false;
                continue;
            }

            rows.Add(new FixtureRow(spec.FileName, line, [.. content.Split(',').Select(c => c.Trim())]));
        }

        return rows;
    }
}
