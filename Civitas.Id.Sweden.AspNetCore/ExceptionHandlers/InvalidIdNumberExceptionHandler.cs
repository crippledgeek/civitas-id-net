using System.Diagnostics;
using System.Text.Json;
using Civitas.Id.Sweden.AspNetCore.Constants;
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
/// endpoint handlers into RFC 9457 <see cref="ProblemDetails"/> 400 responses
/// via <see cref="IProblemDetailsService.TryWriteAsync"/>, so consumer
/// <c>ProblemDetailsOptions.CustomizeProblemDetails</c> hooks and
/// <c>IProblemDetailsWriter</c> registrations apply. Falls back to a
/// hand-written AOT-safe <see cref="Utf8JsonWriter"/> path when the writer
/// throws <see cref="NotSupportedException"/> (indicating
/// <see cref="ProblemDetails"/> is absent from the
/// <c>Microsoft.AspNetCore.Http.Json.JsonOptions</c> resolver chain) or
/// returns <see langword="false"/>.
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
    IProblemDetailsService problemDetailsService,
    ILogger<InvalidIdNumberExceptionHandler> logger) : IExceptionHandler
{
    private const string ProblemContentType = "application/problem+json";
    private const string ProblemTitle = "Invalid Swedish ID number";
    private const string ProblemDetailMessage = "The provided value is not a valid Swedish official ID.";

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

        var pd = new ProblemDetails
        {
            Type = $"{_opts.ProblemDetailsTypeBaseUri}{ProblemTypeFragments.InvalidIdNumber}",
            Title = ProblemTitle,
            Status = StatusCodes.Status400BadRequest,
            Detail = ProblemDetailMessage,
            Extensions =
            {
                ["reason"] = id.Reason.ToString(),
                ["input"] = input,
                ["traceId"] = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier
            }
        };

        try
        {
            var written = await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = pd
            }).ConfigureAwait(false);

            if (written)
            {
                return true;
            }

            LogFallbackEngagedWarning(logger);
            LogFallbackEngagedDebug(logger, "TryWriteAsync returned false (no writer claimed)");
        }
#pragma warning disable CA1031 // Do not catch general exception types — NotSupportedException is the documented signal from JsonSerializerOptions.GetTypeInfo when ProblemDetails is absent from the resolver chain.
        catch (NotSupportedException ex)
#pragma warning restore CA1031
        {
            // DefaultProblemDetailsWriter calls JsonSerializerOptions.GetTypeInfo
            // which throws NotSupportedException (NOT silently returns null) when
            // ProblemDetails is absent from the Http.Json.JsonOptions
            // TypeInfoResolverChain. Engage the hand-written fallback so the 400
            // response still reaches the client.
            LogFallbackEngagedWarning(logger);
            LogFallbackEngagedDebug(logger, ex.Message);
        }

        await WriteHandWrittenFallbackAsync(httpContext, pd, cancellationToken).ConfigureAwait(false);
        return true;
    }

    private static async Task WriteHandWrittenFallbackAsync(
        HttpContext ctx,
        ProblemDetails pd,
        CancellationToken ct)
    {
        ctx.Response.ContentType = ProblemContentType;

        // Hand-written AOT-safe ProblemDetails serialization. Avoids the
        // reflection-based WriteAsJsonAsync overload (IL2026/IL3050).
        // traceId is sourced from pd.Extensions (set at construction in
        // TryHandleAsync) so the wire shape matches the success path.
        // ReSharper disable UseAwaitUsing
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("type", pd.Type);
            writer.WriteString("title", pd.Title);
            writer.WriteNumber("status", pd.Status ?? StatusCodes.Status400BadRequest);
            writer.WriteString("detail", pd.Detail);
            foreach (var (key, value) in pd.Extensions)
            {
                writer.WriteString(key, value?.ToString() ?? string.Empty);
            }

            writer.WriteEndObject();
        }

        buffer.Position = 0;
        await buffer.CopyToAsync(ctx.Response.Body, ct).ConfigureAwait(false);
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Civitas.Id parse failure: {Reason}")]
    private static partial void LogParseFailure(ILogger logger, InvalidIdNumberReason reason);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "Civitas.Id ProblemDetails fallback engaged.")]
    private static partial void LogFallbackEngagedWarning(ILogger logger);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Civitas.Id ProblemDetails fallback engaged: {Reason}")]
    private static partial void LogFallbackEngagedDebug(ILogger logger, string reason);
}
