using System.ComponentModel.DataAnnotations;
using Civitas.Id.Sweden.Format;
using JetBrains.Annotations;

namespace Civitas.Id.Sweden.AspNetCore.Options;

/// <summary>Options for the Civitas.Id.Sweden ASP.NET Core integration.</summary>
[PublicAPI]
public sealed class CivitasIdSwedenAspNetCoreOptions
{
    /// <summary>
    /// The format used when serializing Swedish ID numbers to JSON.
    /// Defaults to <see cref="PnrFormat.LongFormat"/>.
    /// </summary>
    [EnumDataType(typeof(PnrFormat))]
    public PnrFormat JsonFormat { get; set; } = PnrFormat.LongFormat;

    /// <summary>
    /// Optional delegate to further redact the ID-number input before it
    /// appears in ProblemDetails responses or log output. When set, the
    /// delegate receives the exception's built-in PII-safe redacted form
    /// and may produce a stricter redaction.
    /// </summary>
    /// <remarks>
    /// Not bindable from appsettings.json (delegate). Set via the
    /// <see cref="Action{T}"/> overload of <c>AddCivitasIdSwedenAspNetCore</c>.
    /// </remarks>
    public Func<string, string>? RedactInput { get; set; }
}
