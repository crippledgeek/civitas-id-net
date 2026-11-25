namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>Names samples by their position, because a generated number must not be printed.</summary>
internal static class SampleIndexes
{
    /// <summary>The positions of the samples that <paramref name="matches" /> holds for.</summary>
    public static IReadOnlyList<int> IndexesWhere<T>(this IEnumerable<T> samples, Func<T, bool> matches)
    {
        return [.. samples.Index().Where(sample => matches(sample.Item)).Select(sample => sample.Index)];
    }
}
