using System.Collections;
using System.Reflection;

namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>
///     A method that supplies the rows of one data file to tests, with the number of rows it must supply. Test
///     methods and the checks of the sources call the same method, so a source cannot be emptied without a failure.
/// </summary>
internal sealed record RowSource
{
    /// <summary>The class that declares the data source.</summary>
    public required Type Provider { get; init; }

    /// <summary>The name of the public static data source method.</summary>
    public required string Method { get; init; }

    /// <summary>The file the rows come from.</summary>
    public required FixtureSpec File { get; init; }

    /// <summary>The pinned number of rows the source supplies. A count, so a plain <see cref="int" />.</summary>
    public required int Rows { get; init; }

    /// <summary>The place of the source in the test code.</summary>
    public string Name => $"{Provider.Name}.{Method}";

    /// <summary>How many rows the source supplies now.</summary>
    public int Supplied()
    {
        var source = Provider.GetMethod(Method, BindingFlags.Public | BindingFlags.Static)
                     ?? throw new InvalidOperationException($"{Name} is not a public static method.");
        return ((IEnumerable)(source.Invoke(null, null) ?? throw new InvalidOperationException($"{Name} yields no sequence."))).Cast<object>().Count();
    }
}
