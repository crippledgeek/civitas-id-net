using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Civitas.Id.Sweden.AspNetCore.Endpoints;

/// <summary>
/// Endpoint convention extensions for advertising the Civitas.Id.Sweden
/// 400 <see cref="ProblemDetails"/> response in OpenAPI metadata.
/// </summary>
[PublicAPI]
public static class CivitasIdSwedenEndpointConventionExtensions
{
    /// <summary>
    /// Adds a 400 <see cref="ProblemDetails"/> response advertisement to the
    /// target endpoint(s). Apply on individual endpoints or <c>MapGroup</c>
    /// calls that accept typed Swedish ID parameters so OpenAPI documents
    /// (and downstream Swagger UI / Scalar viewers) expose the error contract
    /// produced by <c>InvalidIdNumberExceptionHandler</c>.
    /// </summary>
    /// <typeparam name="TBuilder">The endpoint convention builder type.</typeparam>
    /// <param name="builder">The endpoint convention builder to extend.</param>
    /// <returns>The same <paramref name="builder"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <see langword="null"/>.
    /// </exception>
    public static TBuilder WithCivitasIdSwedenMetadata<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Add(endpointBuilder => endpointBuilder.Metadata.Add(
            new ProducesResponseTypeMetadata(
                StatusCodes.Status400BadRequest,
                typeof(ProblemDetails),
                ["application/problem+json"])));
        return builder;
    }
}
