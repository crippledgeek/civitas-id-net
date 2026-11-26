namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>
///     Supplies the rows of <typeparamref name="TSource" /> to a test, one test per row. When the source supplies
///     other than its pinned rows, or is pinned at none, the test fails once with a message that names the source
///     and both counts, instead of running for the rows there are.
/// </summary>
/// <typeparam name="TSource">The source.</typeparam>
/// <typeparam name="TRow">The row type, which the test takes as its parameter.</typeparam>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
internal sealed class RowsOfAttribute<TSource, TRow> : DataSourceGeneratorAttribute<TRow>, INamedRowSource
    where TSource : IRowSource<TRow>
    where TRow : FileRow
{
    /// <summary>The rows of the source.</summary>
    /// <exception cref="InvalidDataException">The source supplies other than its pinned rows, or is pinned at none.</exception>
    public static IReadOnlyList<TRow> Rows()
    {
        IReadOnlyList<TRow> rows = [.. TSource.Read()];
        if (TSource.Pinned <= 0 || rows.Count != TSource.Pinned)
        {
            throw new InvalidDataException(
                $"{SourceName} supplies {rows.Count} rows of {TSource.File.FileName}, pinned at {TSource.Pinned}.");
        }

        return rows;
    }

    /// <summary>The name of the source: its type, after the type it is nested in.</summary>
    public static string SourceName => typeof(TSource).DeclaringType is { } declaring
        ? $"{declaring.Name}.{typeof(TSource).Name}"
        : typeof(TSource).Name;

    /// <inheritdoc />
    string INamedRowSource.SourceName => SourceName;

    /// <inheritdoc />
    protected override IEnumerable<Func<TRow>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        return Rows().Select(row => (Func<TRow>)(() => row));
    }
}
