using Civitas.Id.Sweden.AspNetCore.Constants;
using Civitas.Id.Sweden.AspNetCore.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Civitas.Id.Sweden.AspNetCore.ExceptionHandlers;

/// <summary>
/// Gives 400 responses the library's <c>type</c> in place of the framework's default, for minimal
/// APIs and MVC, which both read <see cref="ProblemDetailsOptions.CustomizeProblemDetails" />.
/// </summary>
/// <remarks>
/// A post-configuration, so it sees the app's callback whichever was registered first, and chains
/// it last: the app has the last word. A type from another domain is not replaced. A later
/// post-configuration that assigns the callback replaces both.
/// </remarks>
internal sealed class BadRequestTypeSetup(IOptions<CivitasIdSwedenAspNetCoreOptions> libraryOptions)
    : IPostConfigureOptions<ProblemDetailsOptions>
{
    /// <summary>The framework's <c>type</c> for a 400 without one.</summary>
    private const string FrameworkRfc9110BadRequestType = "https://tools.ietf.org/html/rfc9110#section-15.5.1";

    /// <inheritdoc />
    public void PostConfigure(string? name, ProblemDetailsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var normalize = Normalize;
        options.CustomizeProblemDetails = options.CustomizeProblemDetails is { } app ? normalize + app : normalize;
    }

    private void Normalize(ProblemDetailsContext context)
    {
        var problem = context.ProblemDetails;
        if (problem.Status == StatusCodes.Status400BadRequest
            && (string.IsNullOrEmpty(problem.Type)
                || string.Equals(problem.Type, FrameworkRfc9110BadRequestType, StringComparison.Ordinal)))
        {
            problem.Type = $"{libraryOptions.Value.ProblemDetailsTypeBaseUri}{ProblemTypeFragments.BadRequest}";
        }
    }
}
