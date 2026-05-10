namespace Civitas.Id.Sweden.Tests.Helpers;

internal static class CsvLoader
{
    public static IReadOnlyList<string[]> Load(string filename)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", filename);
        return
        [
            .. File.ReadAllLines(path)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => line.Split(',').Select(c => c.Trim()).ToArray())
        ];
    }

    public static IReadOnlyList<string> LoadColumn(string filename, int columnIndex = 0, bool skipHeader = false)
    {
        var rows = Load(filename);
        var data = skipHeader ? rows.Skip(1) : rows;
        return [.. data.Select(row => row[columnIndex])];
    }
}
