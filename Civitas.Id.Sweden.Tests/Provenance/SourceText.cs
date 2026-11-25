namespace Civitas.Id.Sweden.Tests.Provenance;

/// <summary>The content of one text file, with the path to report it by.</summary>
internal sealed record SourceText(string RelativePath, string Content);
