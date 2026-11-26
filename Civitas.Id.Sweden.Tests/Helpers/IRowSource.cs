namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>
///     The rows of one data file that some tests take as parameters, with the number of rows it must supply. A
///     test takes them through <see cref="RowsOfAttribute{TSource, TRow}" />, which fails the test when the number
///     differs: a data source that supplies nothing makes its tests disappear, and the run still passes.
/// </summary>
/// <typeparam name="TRow">The row type.</typeparam>
internal interface IRowSource<out TRow>
    where TRow : FileRow
{
    /// <summary>The file the rows come from.</summary>
    static abstract FixtureSpec File { get; }

    /// <summary>The number of rows the source must supply. A count, so a plain <see cref="int" />.</summary>
    static abstract int Pinned { get; }

    /// <summary>
    ///     The number of test methods that take their rows from the source. A test method that drops the source
    ///     leaves no trace on the others, so the count is what shows it. A count, so a plain <see cref="int" />.
    /// </summary>
    static abstract int Consumers { get; }

    /// <summary>Reads the rows.</summary>
    static abstract IEnumerable<TRow> Read();
}
