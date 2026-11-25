namespace Civitas.Id.Sweden.AspNetCore.Tests.Integration.TestApp;

/// <summary>Expected ProblemDetails values and the malformed text that provokes them.</summary>
internal static class ProblemTestConstants
{
    internal const string NotAnId = "not-an-id";
    internal const string AppKey = "app";
    internal const string AppValue = "yes";
    internal const string TypeKey = "type";
    internal const string InputKey = "input";
    internal const string TraceIdKey = "traceId";
    internal const string ReasonKey = "reason";
    internal const string LibraryBadRequestType = "https://civitas-id.dev/errors/bad-request";
    internal const string LibraryInvalidIdType = "https://civitas-id.dev/errors/invalid-id-number";
    internal const string FrameworkBadRequestType = "https://tools.ietf.org/html/rfc9110#section-15.5.1";
    internal const string FrameworkNotFoundType = "https://tools.ietf.org/html/rfc9110#section-15.5.5";
}
