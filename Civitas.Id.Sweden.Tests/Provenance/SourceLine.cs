namespace Civitas.Id.Sweden.Tests.Provenance;

/// <summary>
///     Where a number was found. The line is an <see cref="int" /> because it is only ever printed; it is null when
///     the number is in the path itself.
/// </summary>
internal sealed record SourceLine(RelativePath Path, int? Line)
{
    /// <summary>The location as <c>path:line</c>, or the path alone.</summary>
    public override string ToString()
    {
        return Line is null ? Path.ToString() : $"{Path}:{Line}";
    }
}
