using System.Diagnostics;
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
/// endpoint handlers into RFC 9457 <see cref="ProblemDetails"/> 400 responses,
/// written by <see cref="InvalidIdProblemWriter"/>. Consumer
/// <c>ProblemDetailsOptions.CustomizeProblemDetails</c> hooks and
/// <c>IProblemDetailsWriter</c> registrations apply; what they throw leaves
/// the handler.
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
    InvalidIdProblemWriter writer,
    ILogger<InvalidIdNumberExceptionHandler> logger) : IExceptionHandler
{
    private const string ProblemTitle = "Invalid Swedish ID number";
    private const string ProblemDetailMessage = "The provided value is not a valid Swedish official ID.";
    private const string ReasonExtension = "reason";
    private const string InputExtension = "input";
    private const string TraceIdExtension = "traceId";

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

        var problem = new ProblemDetails
        {
            Type = $"{_opts.ProblemDetailsTypeBaseUri}{ProblemTypeFragments.InvalidIdNumber}",
            Title = ProblemTitle,
            Status = StatusCodes.Status400BadRequest,
            Detail = ProblemDetailMessage,
            Extensions =
            {
                [ReasonExtension] = id.Reason.ToString(),
                [InputExtension] = input,
                [TraceIdExtension] = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier
            }
        };

        var context = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = id
        };
        await writer.WriteAsync(context, cancellationToken).ConfigureAwait(false);
        return true;
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Civitas.Id parse failure: {Reason}")]
    private static partial void LogParseFailure(ILogger logger, InvalidIdNumberReason reason);
}
