namespace Civitas.Id.Sweden.Tests.Provenance;

/// <summary>
///     Where a number was found. The path is a <see cref="string" /> and the line an <see cref="int" /> because they
///     are only ever printed.
/// </summary>
internal sealed record SourceLine(string RelativePath, int Line)
{
    /// <summary>The location as <c>path:line</c>.</summary>
    public override string ToString()
    {
        return $"{RelativePath}:{Line}";
    }
}
