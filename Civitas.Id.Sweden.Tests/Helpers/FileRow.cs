namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>
///     One data row of a test file as a test parameter. Its string form is its place, file name and line, because a
///     test's name and a failure print the parameters and the cells hold numbers. Public because test parameters must
///     be.
/// </summary>
public abstract record FileRow
{
    public required string Source { get; init; }

    /// <summary>The place of the row.</summary>
    public sealed override string ToString()
    {
        return Source;
    }
}
