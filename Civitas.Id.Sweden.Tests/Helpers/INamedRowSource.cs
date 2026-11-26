namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>A data source attribute that names the row source it reads, so that a discovered test shows its source.</summary>
internal interface INamedRowSource
{
    /// <summary>The name of the source, as <see cref="ListedRowSource.Name" /> gives it.</summary>
    string SourceName { get; }
}
