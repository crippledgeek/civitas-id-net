namespace Civitas.Id.Sweden.Tests.Provenance;

/// <summary>One text file. The content is a <see cref="string" /> because any text at all is valid input to a scan.</summary>
internal sealed record SourceText(RelativePath Path, string Content);
