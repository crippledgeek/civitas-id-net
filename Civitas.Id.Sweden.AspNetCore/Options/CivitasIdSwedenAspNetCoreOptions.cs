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
    /// Base URI prepended to per-reason error-type fragments in RFC 9457
    /// ProblemDetails responses. Must be a non-empty absolute URI ending
    /// with a forward slash. Default: <c>https://civitas-id.dev/errors/</c>.
    /// </summary>
    /// <remarks>
    /// Typed as <see cref="string"/> rather than <see cref="Uri"/> so the
    /// value binds cleanly from <c>appsettings.json</c> without a custom
    /// converter; validated at startup via the options validation chain.
    /// </remarks>
    [Required(AllowEmptyStrings = false)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1056:URI-like properties should not be strings",
        Justification = "Bound from IConfiguration; Uri does not round-trip through JSON config without a converter.")]
    public string ProblemDetailsTypeBaseUri { get; set; } = "https://civitas-id.dev/errors/";

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
    /// <example>
    /// Bridging to Microsoft.Extensions.Compliance.Redaction:
    /// <code>
    /// var provider = sp.GetRequiredService&lt;IRedactorProvider&gt;();
    /// opts.RedactInput = s => provider.GetRedactor(DataClassifications.PrivateData)
    ///     .Redact(s, destination: stackalloc char[s.Length]);
    /// </code>
    /// </example>
    public Func<string, string>? RedactInput { get; set; }
}
