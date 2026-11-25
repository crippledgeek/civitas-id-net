using Microsoft.AspNetCore.Mvc;

namespace Civitas.Id.Sweden.AspNetCore.Tests.Integration.TestApp;

/// <summary>MVC responses that carry ProblemDetails, for the customization tests.</summary>
[ApiController]
[Route(Route)]
public sealed class ProblemProbeController : ControllerBase
{
    internal const string Route = "problem-probe";
    internal const string BadRequestAction = "bad-request";
    internal const string NotFoundAction = "not-found";
    internal const string ValidatedAction = "validated";

    /// <summary>A 400 without a model error.</summary>
    [HttpGet(BadRequestAction)]
    public IActionResult AnswerBadRequest() => BadRequest();

    /// <summary>A 404.</summary>
    [HttpGet(NotFoundAction)]
    public IActionResult AnswerNotFound() => NotFound();

    /// <summary>A 400 from model validation.</summary>
    [HttpGet(ValidatedAction)]
    public IActionResult Validated([FromQuery] int number) => Ok(number);
}
