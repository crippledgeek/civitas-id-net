using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace Civitas.Id.Sweden.AspNetCore.ExceptionHandlers;

/// <summary>
/// Writes a problem through <see cref="IProblemDetailsService"/>, or as JSON when no registered
/// writer claims the response, for example because the client accepts only <c>text/plain</c>.
/// </summary>
/// <remarks>
/// <see cref="ProblemDetailsOptions.CustomizeProblemDetails"/> runs in the framework's writers, so
/// it has not run when none of them claims the response; it runs here instead, before anything is
/// written. What it throws is not caught, so a body the app meant to change is never sent unchanged.
/// </remarks>
internal sealed class InvalidIdProblemWriter(
    IProblemDetailsService problemDetailsService,
    IOptions<ProblemDetailsOptions> problemDetailsOptions,
    IOptions<JsonOptions> jsonOptions)
{
    private const string ProblemContentType = "application/problem+json";

    /// <summary>Writes the problem in <paramref name="context"/>.</summary>
    /// <remarks>
    /// The app's registered metadata is used, as in the framework's own writer: the overloads that
    /// take no metadata are not AOT-safe (IL2026, IL3050).
    /// </remarks>
    public async Task WriteAsync(ProblemDetailsContext context, CancellationToken cancellationToken)
    {
        if (await problemDetailsService.TryWriteAsync(context).ConfigureAwait(false))
        {
            return;
        }

        problemDetailsOptions.Value.CustomizeProblemDetails?.Invoke(context);

        var response = context.HttpContext.Response;
        var problem = context.ProblemDetails;
        problem.Status ??= response.StatusCode;
        var typeInfo = jsonOptions.Value.SerializerOptions.GetTypeInfo(problem.GetType());
        await response.WriteAsJsonAsync(problem, typeInfo, ProblemContentType, cancellationToken).ConfigureAwait(false);
    }
}
