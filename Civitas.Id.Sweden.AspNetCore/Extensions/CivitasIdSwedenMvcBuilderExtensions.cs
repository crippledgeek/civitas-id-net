using Civitas.Id.Sweden.AspNetCore.Options;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Civitas.Id.Sweden.AspNetCore.Extensions;

// ReSharper disable ConvertToExtensionBlock
// Rationale: C# 14 extension(T) syntax triggers CA1034 false positive
// (dotnet/sdk#51681 fix is .NET 11 SDK only). Microsoft has not migrated
// its own DI extension methods to the new syntax. IL is identical.

/// <summary>
/// MVC-specific Civitas.Id.Sweden integration extensions for
/// <see cref="IMvcBuilder"/>.
/// </summary>
[PublicAPI]
public static class CivitasIdSwedenMvcBuilderExtensions
{
    /// <summary>
    /// Adds MVC-specific Civitas.Id.Sweden integration: sets
    /// <see cref="ApiBehaviorOptions.ClientErrorMapping"/>[400] so that the
    /// <c>[ApiController]</c> auto-400 path emits a
    /// <see cref="ValidationProblemDetails"/> with the library's normalized
    /// <c>type</c> URI. <c>Mvc.JsonOptions</c> converter wiring is already
    /// registered by <c>AddCivitasIdSwedenAspNetCore()</c> and activates
    /// automatically when <c>AddControllers()</c> is called.
    /// </summary>
    /// <remarks>
    /// Call after <c>AddControllers()</c>. Not needed for minimal-API-only
    /// applications; <c>AddCivitasIdSwedenAspNetCore()</c> alone covers them.
    /// </remarks>
    /// <param name="builder">The <see cref="IMvcBuilder"/> to extend.</param>
    /// <returns>The same <paramref name="builder"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <see langword="null"/>.
    /// </exception>
    public static IMvcBuilder AddCivitasIdSweden(this IMvcBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddOptions<ApiBehaviorOptions>()
            .Configure<IOptions<CivitasIdSwedenAspNetCoreOptions>>(static (apiOpts, civitasOpts) =>
            {
                apiOpts.ClientErrorMapping[StatusCodes.Status400BadRequest] = new ClientErrorData
                {
                    Title = "Bad Request",
                    Link = $"{civitasOpts.Value.ProblemDetailsTypeBaseUri}bad-request"
                };
            });

        return builder;
    }
}
