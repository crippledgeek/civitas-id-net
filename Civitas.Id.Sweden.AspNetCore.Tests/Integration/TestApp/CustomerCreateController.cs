using System.Diagnostics.CodeAnalysis;
using Civitas.Id.Sweden.Core;
using Microsoft.AspNetCore.Mvc;

namespace Civitas.Id.Sweden.AspNetCore.Tests.Integration.TestApp;

/// <summary>
/// MVC controller that exercises the <c>[FromBody]</c> STJ-parse-failure path
/// using a DTO with a strongly-typed <see cref="PersonalId"/> property. The
/// failure surface here is JSON deserialization (the converter throws
/// <c>InvalidIdNumberException</c>), NOT ModelState validation, so the
/// response shape is <c>ProblemDetails</c> (no <c>errors</c> dict).
/// </summary>
[ApiController]
[Route("customer-create")]
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes",
    Justification = "Materialized by MVC via AddApplicationPart.")]
public sealed class CustomerCreateController : ControllerBase
{
    /// <summary>DTO with a strongly-typed PersonalId property — failures surface during JSON parsing.</summary>
    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global",
        Justification = "Accessed via JSON serializer.")]
    public sealed record CustomerCreateDto(PersonalId Id);

    /// <summary>Echoes the posted DTO; JSON parse failure on Id surfaces as ProblemDetails via the exception handler.</summary>
    [HttpPost]
    public ActionResult<CustomerCreateDto> Post([FromBody] CustomerCreateDto dto) => Ok(dto);
}
