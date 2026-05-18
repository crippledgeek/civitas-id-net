using System.Diagnostics.CodeAnalysis;
using Civitas.Id.Sweden.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Civitas.Id.Sweden.AspNetCore.Tests.Integration.TestApp;

/// <summary>
/// Top-level MVC controller used by <c>CustomizeProblemDetailsTests</c> to
/// exercise the <c>[ApiController]</c> auto-400 path and its
/// <see cref="ValidationProblemDetails"/> response.
/// ASP.NET Core's default <c>ControllerFeatureProvider</c> excludes nested
/// types — keep this at namespace scope.
/// </summary>
[ApiController]
[Route("legacy")]
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes",
    Justification = "Materialized by MVC via AddApplicationPart.")]
public sealed class LegacyController : ControllerBase
{
    /// <summary>DTO used to trigger ValidationProblemDetails through [ValidPersonalId].</summary>
    /// <remarks>
    /// Per ASP.NET Core record-type validation metadata rules, the attribute MUST
    /// target the primary-constructor parameter (no <c>property:</c> prefix) so the
    /// ObjectModelValidator picks it up at the ParameterDescriptor.
    /// </remarks>
    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global",
        Justification = "Accessed via JSON serializer and ApiController model validation.")]
    public sealed record LegacyDto([ValidPersonalId] string Id);

    /// <summary>Echoes the posted DTO when valid; ApiController emits 400 ValidationProblemDetails otherwise.</summary>
    [HttpPost]
    public ActionResult<LegacyDto> Post([FromBody] LegacyDto dto) => Ok(dto);
}
