namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>
///     An <see cref="IRowSource{TRow}" /> in the list of sources, read through its pin, so that the list can be
///     checked against the data files and against the tests that were discovered.
/// </summary>
internal sealed class ListedRowSource
{
    private ListedRowSource(string name, FixtureSpec file, int consumers, Func<IReadOnlyList<FileRow>> read)
    {
        Name = name;
        File = file;
        Consumers = consumers;
        Read = read;
    }

    /// <summary>The name of the source, as <see cref="RowsOfAttribute{TSource, TRow}" /> gives it.</summary>
    public string Name { get; }

    /// <summary>The file the source declares.</summary>
    public FixtureSpec File { get; }

    /// <summary>The number of test methods pinned to take their rows from the source.</summary>
    private int Consumers { get; }

    /// <summary>Reads the rows of the source; throws when they differ from its pin.</summary>
    private Func<IReadOnlyList<FileRow>> Read { get; }

    /// <summary>The source <typeparamref name="TSource" />.</summary>
    public static ListedRowSource Of<TSource, TRow>()
        where TSource : IRowSource<TRow>
        where TRow : FileRow
    {
        return new ListedRowSource(RowsOfAttribute<TSource, TRow>.SourceName, TSource.File, TSource.Consumers,
            RowsOfAttribute<TSource, TRow>.Rows);
    }

    /// <summary>
    ///     The files whose rows <paramref name="sources" /> do not supply exactly once each: a row is missing, read
    ///     twice, or read by a source that declares another file.
    /// </summary>
    public static IEnumerable<FixtureSpec> FilesNotSuppliedExactlyOnce(IEnumerable<ListedRowSource> sources)
    {
        return sources.GroupBy(source => source.File)
            .Where(file => !PlacesOf(file.SelectMany(source => source.Read()).Select(row => row.Source))
                .SequenceEqual(PlacesOf(CsvLoader.Rows(file.Key).Select(row => row.Source))))
            .Select(file => file.Key);
    }

    /// <summary>The data files that none of <paramref name="sources" /> reads.</summary>
    public static IEnumerable<FixtureSpec> UnreadFiles(IEnumerable<ListedRowSource> sources)
    {
        var read = sources.Select(source => source.File).ToHashSet();
        return Fixtures.All.Where(file => !read.Contains(file));
    }

    /// <summary>
    ///     What keeps <paramref name="discovered" /> from being exactly the tests of <paramref name="listed" />: a
    ///     source that feeds tests but is not listed, a listed source with rows that no test takes, and a listed
    ///     source taken by another number of test methods than it pins. Empty when they agree.
    /// </summary>
    public static IEnumerable<string> RowsNotDiscovered(IReadOnlyList<ListedRowSource> listed, IEnumerable<DiscoveredRow> discovered)
    {
        var bySource = discovered.ToLookup(row => row.SourceName, StringComparer.Ordinal);
        var names = listed.Select(source => source.Name).ToHashSet(StringComparer.Ordinal);

        var unlisted = bySource.Select(source => source.Key).Where(name => !names.Contains(name))
            .Order(StringComparer.Ordinal).Select(name => $"{name} feeds tests but is not listed");
        var untaken = listed
            .Select(source => (source.Name,
                Absent: source.Read().Select(row => row.Source).Except(bySource[source.Name].Select(row => row.Place)).ToList()))
            .Where(source => source.Absent.Count > 0)
            .Select(source => $"rows of {source.Name} that no test takes: {source.Absent.Count}, the first {source.Absent[0]}");
        var miscounted = listed
            .Select(source => (source.Name, source.Consumers,
                Methods: bySource[source.Name].Select(row => row.TestMethod).Distinct(StringComparer.Ordinal).Count()))
            .Where(source => source.Methods != source.Consumers)
            .Select(source => $"test methods fed by {source.Name}: {source.Methods}, pinned at {source.Consumers}");
        return unlisted.Concat(untaken).Concat(miscounted);
    }

    private static IEnumerable<string> PlacesOf(IEnumerable<string> places)
    {
        return places.Order(StringComparer.Ordinal);
    }
}
