using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Civitas.Id.Sweden.AspNetCore.ExceptionHandlers;

/// <summary>
/// Makes <c>UseExceptionHandler()</c> answer a <see cref="BadHttpRequestException"/> with its own
/// status instead of 500. ASP.NET Core throws it for a malformed parameter when
/// <c>RouteHandlerOptions.ThrowOnBadRequest</c> is set, which is the default in Development.
/// </summary>
/// <remarks>
/// An app that configures <c>StatusCodeSelector</c>, <c>ExceptionHandler</c> or
/// <c>ExceptionHandlingPath</c> keeps its own choice. With a path, a selector would also disable the
/// framework's check that the error page exists; a path set by a post-configuration registered after
/// this one is not seen, and an <c>IExceptionHandler</c> that answers 404 without handling the
/// exception is not caught by that check either.
/// The mapping is the one ASP.NET Core adopted for a later version (dotnet/aspnetcore#68632); drop
/// this type when the target framework includes it.
/// </remarks>
internal sealed class BadRequestStatusSetup : IPostConfigureOptions<ExceptionHandlerOptions>
{
    /// <inheritdoc />
    public void PostConfigure(string? name, ExceptionHandlerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options is { StatusCodeSelector: null, ExceptionHandler: null, ExceptionHandlingPath.HasValue: false })
        {
            options.StatusCodeSelector = StatusOf;
        }
    }

    private static int StatusOf(Exception exception) => exception is BadHttpRequestException badRequest
        ? badRequest.StatusCode
        : StatusCodes.Status500InternalServerError;
}
