using System.Text.Json;
using Civitas.Id.Sweden.AspNetCore.Options;
using Civitas.Id.Sweden.Errors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Civitas.Id.Sweden.AspNetCore.ExceptionHandlers;

/// <summary>
/// Translates <see cref="InvalidIdNumberException"/> instances thrown from
/// endpoint handlers into RFC 9457 <see cref="ProblemDetails"/> 400 responses,
/// with PII-safe input redaction in the response body.
/// </summary>
/// <remarks>
/// The exception itself never stores the raw input (see
/// <see cref="InvalidIdNumberException.RedactedInput"/>). This handler uses
/// the built-in redacted form by default, and applies the caller-provided
/// <see cref="CivitasIdSwedenAspNetCoreOptions.RedactInput"/> delegate (if any)
/// for additional redaction.
/// </remarks>
internal sealed partial class InvalidIdNumberExceptionHandler(
    IOptions<CivitasIdSwedenAspNetCoreOptions> options,
    ILogger<InvalidIdNumberExceptionHandler> logger) : IExceptionHandler
{
    private const string ProblemContentType = "application/problem+json";
    private const string ProblemType = "https://civitas-id.dev/errors/invalid-id-number";
    private const string ProblemTitle = "Invalid Swedish ID number";
    private const string ProblemDetail = "The provided value is not a valid Swedish official ID.";

    private readonly CivitasIdSwedenAspNetCoreOptions _opts = options.Value;

    /// <summary>
    /// Inspects <paramref name="exception"/> and, if it is an
    /// <see cref="InvalidIdNumberException"/>, writes a 400 ProblemDetails
    /// response and returns <see langword="true"/>. Returns <see langword="false"/>
    /// for any other exception type so the next handler in the chain can run.
    /// </summary>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is not InvalidIdNumberException id)
        {
            return false;
        }

        LogParseFailure(logger, id.Reason);

        var redacted = id.RedactedInput ?? "[null]";
        var input = _opts.RedactInput is { } redact ? redact(redacted) : redacted;

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        httpContext.Response.ContentType = ProblemContentType;

        // Hand-written AOT-safe ProblemDetails serialization. Avoids the
        // reflection-based WriteAsJsonAsync overload (IL2026/IL3050) and
        // the difficulty of source-generating the IDictionary-shaped
        // Extensions property.
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("type", ProblemType);
            writer.WriteString("title", ProblemTitle);
            writer.WriteNumber("status", StatusCodes.Status400BadRequest);
            writer.WriteString("detail", ProblemDetail);
            writer.WriteString("reason", id.Reason.ToString());
            writer.WriteString("input", input);
            writer.WriteEndObject();
        }

        buffer.Position = 0;
        await buffer.CopyToAsync(httpContext.Response.Body, cancellationToken).ConfigureAwait(false);
        return true;
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Civitas.Id parse failure: {Reason}")]
    private static partial void LogParseFailure(ILogger logger, InvalidIdNumberReason reason);
}
