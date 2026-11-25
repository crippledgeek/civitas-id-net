using System.Diagnostics.CodeAnalysis;
using Civitas.Id.Sweden.Core;
using Microsoft.AspNetCore.Mvc;

namespace Civitas.Id.Sweden.AspNetCore.Tests.Integration.TestApp;

/// <summary>
/// MVC controller that exercises the <c>[FromBody]</c> STJ-parse-failure path
/// using a DTO with a strongly-typed <see cref="PersonalId"/> property. MVC
/// records the converter's exception in ModelState, so the response shape is
/// <see cref="ValidationProblemDetails"/>.
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

    /// <summary>Echoes the posted DTO; JSON parse failure on Id surfaces as ValidationProblemDetails.</summary>
    [HttpPost]
    public ActionResult<CustomerCreateDto> Post([FromBody] CustomerCreateDto dto) => Ok(dto);
}
