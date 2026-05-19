namespace Civitas.Id.Sweden.AspNetCore.Constants;

/// <summary>
/// String fragments appended to <c>CivitasIdSwedenAspNetCoreOptions.ProblemDetailsTypeBaseUri</c>
/// to form RFC 9457 ProblemDetails <c>type</c> URIs. Shared by the
/// CustomizeProblemDetails hook and the <c>InvalidIdNumberExceptionHandler</c>
/// so neither side carries a duplicate magic string.
/// </summary>
internal static class ProblemTypeFragments
{
    /// <summary>
    /// Fragment used for generic 400 responses that the library normalizes
    /// (framework RFC 9110 default and null/empty Type values).
    /// </summary>
    public const string BadRequest = "bad-request";

    /// <summary>
    /// Fragment used for <c>InvalidIdNumberException</c> 400 responses
    /// emitted by <c>InvalidIdNumberExceptionHandler</c>.
    /// </summary>
    public const string InvalidIdNumber = "invalid-id-number";
}
