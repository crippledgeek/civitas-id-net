using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace Civitas.Id.Sweden.AspNetCore.Tests.Integration;

/// <summary>
/// Verifies the four distinct response-body shapes emitted for the four
/// Gap-D failure surfaces. All shapes share the same <c>type</c> URI base
/// (<c>ProblemDetailsTypeBaseUri</c>) but differ structurally — see the
/// AspNetCore package README "Response shapes" section for the full table.
/// </summary>
public sealed class ResponseShapeTests
{
    /// <summary>Row 1 — minimal-API <c>IParsable&lt;PersonalId&gt;</c> route-parse failure.</summary>
    /// <remarks>
    /// Observed shape: plain <see cref="ProblemDetails"/> with the library's
    /// configured <c>bad-request</c> type URI and no <c>errors</c> dictionary.
    /// Although <a href="https://github.com/dotnet/aspnetcore/pull/62066">aspnetcore#62066</a>
    /// shipped <see cref="HttpValidationProblemDetails"/> for minimal-API
    /// validation in .NET 10 GA, route-parameter parse failures for typed
    /// <c>IParsable&lt;T&gt;</c> bindings still surface through the status-code
    /// 400 path which the library customizes via <c>CustomizeProblemDetails</c>
    /// — the body therefore carries the type URI without a field-level
    /// <c>errors</c> dict.
    /// </remarks>
    [Test]
    public async Task Row1_MinimalApi_RouteParseFail_EmitsProblemDetails()
    {
        await using var fx = await IntegrationTestFixture.CreateAsync();
        var resp = await fx.Client.GetAsync(new Uri("/customers/19811218bad9", UriKind.Relative));

        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);

        // Read once into a string so we can both ReadFromJson and probe raw JSON.
        var body = await resp.Content.ReadAsStringAsync();
        var pd = System.Text.Json.JsonSerializer.Deserialize<ProblemDetails>(body);
        await Assert.That(pd).IsNotNull();
        await Assert.That(pd!.Type).IsNotNull();
        await Assert.That(pd.Type!).StartsWith("https://civitas-id.dev/errors/");

        // Distinguishing property: Row 1 emits ProblemDetails, NOT
        // HttpValidationProblemDetails. The "errors" property MUST be absent.
        // (PR aspnetcore#62066 introduced ValidationEndpointFilterFactory which
        // emits errors-dict on AddValidation() routes; route binding via
        // IParsable<T> does not invoke that filter — empirically confirmed
        // against aspnetcore release/10.0.)
        using var doc = System.Text.Json.JsonDocument.Parse(body);
        await Assert.That(doc.RootElement.TryGetProperty("errors", out _)).IsFalse();
    }

    /// <summary>Row 2 — minimal API endpoint explicitly throws <c>InvalidIdNumberException</c>.</summary>
    /// <remarks>
    /// The <c>IParsable.Parse</c> path collapses every <c>TryParse=false</c>
    /// outcome to <see cref="Errors.InvalidIdNumberReason.InvalidFormat"/>;
    /// only the converter / IsValid surfaces distinguish among reasons. The
    /// reason value here is therefore <c>InvalidFormat</c> regardless of
    /// whether the structural defect is a checksum mismatch or a literal
    /// formatting issue.
    /// </remarks>
    [Test]
    public async Task Row2_MinimalApi_ThrownException_EmitsProblemDetails_WithReasonExtension()
    {
        await using var fx = await IntegrationTestFixture.CreateAsync();
        // /customers-strict/{idString} calls PersonalId.Parse(idString) inside the
        // handler — the throw routes through InvalidIdNumberExceptionHandler which
        // emits ProblemDetails (NOT HttpValidationProblemDetails) with reason+input
        // extensions.
        var resp = await fx.Client.GetAsync(new Uri("/customers-strict/19811218bad9", UriKind.Relative));

        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        var pd = await resp.Content.ReadFromJsonAsync<ProblemDetails>();
        await Assert.That(pd).IsNotNull();
        await Assert.That(pd!.Extensions.ContainsKey("reason")).IsTrue();
        await Assert.That(pd.Extensions["reason"]?.ToString()).IsEqualTo("InvalidFormat");
        await Assert.That(pd.Extensions.ContainsKey("input")).IsTrue();
    }

    /// <summary>Row 3 — MVC <c>[ApiController]</c> ModelState validation failure.</summary>
    [Test]
    public async Task Row3_MvcController_ApiControllerModelStateFail_EmitsValidationProblemDetails()
    {
        await using var fx = await IntegrationTestFixture.CreateAsync();
        var resp = await fx.Client.PostAsJsonAsync(
            new Uri("/legacy", UriKind.Relative),
            new { id = "19811218bad9" });

        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        var vpd = await resp.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        await Assert.That(vpd).IsNotNull();
        await Assert.That(vpd!.Errors).IsNotEmpty();
    }

    /// <summary>Row 4 — MVC <c>[FromBody]</c> JSON parse failure (typed PersonalId property).</summary>
    [Test]
    public async Task Row4_MvcController_FromBodyJsonParseFail_EmitsProblemDetails()
    {
        await using var fx = await IntegrationTestFixture.CreateAsync();
        // CustomerCreateController has a strongly-typed PersonalId property in its DTO;
        // an invalid value triggers the JSON converter to throw InvalidIdNumberException
        // during deserialization — surfaced via the exception handler as ProblemDetails,
        // NOT as ValidationProblemDetails (no errors dict).
        using var content = new StringContent(
            "{\"id\":\"19811218bad9\"}", Encoding.UTF8, "application/json");
        var resp = await fx.Client.PostAsync(new Uri("/customer-create", UriKind.Relative), content);

        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        var pd = await resp.Content.ReadFromJsonAsync<ProblemDetails>();
        await Assert.That(pd).IsNotNull();
        await Assert.That(pd!.Type).IsNotNull();
        await Assert.That(pd.Type!).StartsWith("https://civitas-id.dev/errors/");
    }
}
