using Civitas.Id.Sweden.Tests.Helpers;

namespace Civitas.Id.Sweden.Tests.Rows;

/// <summary>
///     Checks once, after discovery of a run that is not filtered, the discovered tests against
///     <see cref="RowSources.All" />, and fails the run with a message that names each difference:
///     <list type="bullet">
///         <item>a source that feeds a discovered test but is not listed;</item>
///         <item>a row of a listed source that is the parameter of no discovered test fed by that source;</item>
///         <item>a listed source fed to another number of test methods than its pinned consumers.</item>
///     </list>
///     A test that loses its data source does not exist, so no test can see this. A filtered run discovers only some
///     tests and is not checked; neither is a test whose rows come from a data source that is not a row source.
/// </summary>
public static class RowDiscovery
{
    /// <summary>Whether the hook checked the discovered tests in this run.</summary>
    public static bool Checked { get; private set; }

    /// <summary>Throws, failing the run, when the discovered row tests and the listed sources differ.</summary>
    /// <exception cref="InvalidDataException">They differ; the message names each difference.</exception>
    [After(TestDiscovery)]
    public static void EveryListedRowIsATest(TestDiscoveryContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.TestFilter is not null)
        {
            return;
        }

        var differences = ListedRowSource.RowsNotDiscovered(RowSources.All, DiscoveredRows(context.AllTests)).ToList();
        if (differences.Count > 0)
        {
            throw new InvalidDataException(string.Join("; ", differences));
        }

        Checked = true;
    }

    private static IEnumerable<DiscoveredRow> DiscoveredRows(IEnumerable<TestContext> tests)
    {
        return tests.Select(test => test.Metadata.TestDetails)
            .SelectMany(details => details.GetAllAttributes().OfType<INamedRowSource>()
                .SelectMany(source => details.TestMethodArguments.OfType<FileRow>()
                    .Select(row => new DiscoveredRow(source.SourceName, $"{details.ClassType.FullName}.{details.MethodName}", row.Source))));
    }
}
