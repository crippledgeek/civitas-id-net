using System.Reflection;
using Civitas.Id.Sweden.Tests.Helpers;

namespace Civitas.Id.Sweden.Tests.Rows;

/// <summary>
///     That the data files are read by row tests, and that no source can be emptied: a test that has no rows does not
///     run, and passes.
/// </summary>
public class RowSourcesTests
{
    [Test]
    public async Task EverySource_SuppliesExactlyItsPinnedRows()
    {
        var wrong = RowSources.All
            .Select(source => (source.Name, Supplied: source.Supplied(), Pinned: source.Rows))
            .Where(source => source.Supplied != source.Pinned)
            .Select(source => $"{source.Name} supplies {source.Supplied} rows, expected {source.Pinned}");

        await Assert.That(RowSources.All.Count).IsGreaterThan(0);
        await Assert.That(RowSources.All.Where(source => source.Rows <= 0).Select(source => source.Name)).IsEmpty();
        await Assert.That(wrong).IsEmpty();
    }

    [Test]
    public async Task EverySource_IsTheDataSourceOfATest()
    {
        var used = DataSourcesOfTests();
        var unused = RowSources.All.Where(source => !used.Contains((source.Provider, source.Method)))
            .Select(source => source.Name);

        await Assert.That(used.Count).IsGreaterThan(0);
        await Assert.That(unused).IsEmpty();
    }

    [Test]
    public async Task EveryDataSourceOfARowTest_IsRegistered()
    {
        var registered = RowSources.All.Select(source => (Provider: (Type?)source.Provider, source.Method)).ToHashSet();
        var unregistered = DataSourcesOfTests()
            .Where(source => source.Provider?.Namespace == typeof(RowSources).Namespace)
            .Where(source => !registered.Contains(source))
            .Select(source => $"{source.Provider?.Name}.{source.Method}");

        await Assert.That(unregistered).IsEmpty();
    }

    [Test]
    public async Task SourcesOfAFile_SupplyEveryRowOfItExactlyOnce()
    {
        var wrong = RowSources.All.GroupBy(source => source.File)
            .Where(file => file.Sum(source => source.Rows) != file.Key.DataRows)
            .Select(file => file.Key.FileName);

        await Assert.That(wrong).IsEmpty();
    }

    private static HashSet<(Type? Provider, string Method)> DataSourcesOfTests()
    {
        return
        [
            .. TestMethods()
                .SelectMany(method => method.GetCustomAttributes<MethodDataSourceAttribute>()
                    .Select(attribute => (Provider: attribute.ClassProvidingDataSource ?? method.DeclaringType,
                        Method: attribute.MethodNameProvidingDataSource)))
        ];
    }

    private static IEnumerable<MethodInfo> TestMethods()
    {
        return typeof(RowSources).Assembly.GetTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            .Where(method => method.GetCustomAttribute<TestAttribute>() is not null);
    }
}
