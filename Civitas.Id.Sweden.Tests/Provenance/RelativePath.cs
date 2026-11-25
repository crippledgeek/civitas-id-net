namespace Civitas.Id.Sweden.Tests.Provenance;

/// <summary>A path relative to the repository root. Printing it hides the digits when there are enough for a number.</summary>
internal sealed record RelativePath
{
    private const int ShortestNumberLength = 10;
    private const char HiddenDigit = '#';

    public RelativePath(string value)
    {
        if (value.Length == 0 || Path.IsPathRooted(value))
            throw new ArgumentException("A relative path is neither empty nor rooted.", nameof(value));
        Value = value;
    }

    /// <summary>The path as Git prints it, with <c>/</c> between its parts.</summary>
    public string Value { get; }

    /// <summary>The path, with its digits hidden when it has at least ten of them.</summary>
    public override string ToString()
    {
        return Value.Count(char.IsDigit) < ShortestNumberLength
            ? Value
            : string.Concat(Value.Select(c => char.IsDigit(c) ? HiddenDigit : c));
    }
}
