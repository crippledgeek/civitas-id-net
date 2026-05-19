using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace Civitas.Id.Sweden.AspNetCore.Tests.Integration;

/// <summary>
/// Locks the cross-surface ProblemDetails <c>type</c> URI normalization (Gap B).
/// Verifies that <c>CustomizeProblemDetails</c> fires for both minimal API
/// <see cref="ProblemDetails"/> and MVC <see cref="ValidationProblemDetails"/>,
/// per <c>DefaultProblemDetailsFactory.ApplyProblemDetailsDefaults</c>
/// (aspnetcore release/10.0 source).
/// </summary>
public sealed class CustomizeProblemDetailsTests
{
    [Test]
    public async Task MinimalApi_400_HasNormalizedType()
    {
        await using var fx = await IntegrationTestFixture.CreateAsync();
        // Use a route-bind failure path (IParsable<T> route binding) that doesn't
        // go through IExceptionHandler — the framework's own 400 path produces an
        // empty ProblemDetails that CustomizeProblemDetails must enrich.
        var resp = await fx.Client.GetAsync(new Uri("/customers/19811218bad9", UriKind.Relative));
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        var pd = await resp.Content.ReadFromJsonAsync<ProblemDetails>();
        await Assert.That(pd).IsNotNull();
        await Assert.That(pd!.Type).IsNotNull();
        await Assert.That(pd.Type!).StartsWith("https://civitas-id.dev/errors/");
    }

    [Test]
    public async Task MvcController_ValidationProblemDetails_HasNormalizedType()
    {
        await using var fx = await IntegrationTestFixture.CreateAsync();
        var resp = await fx.Client.PostAsJsonAsync(
            new Uri("/legacy", UriKind.Relative),
            new { id = "19811218bad9" });
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        var vpd = await resp.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        await Assert.That(vpd).IsNotNull();
        await Assert.That(vpd!.Type).IsNotNull();
        await Assert.That(vpd.Type!).StartsWith("https://civitas-id.dev/errors/");
        await Assert.That(vpd.Errors).ContainsKey("Id");
        await Assert.That(vpd.Errors["Id"]).IsNotEmpty();
    }

    [Test]
    public async Task Consumer_CustomizeProblemDetails_Type_NotOverwritten()
    {
        await using var fx = await IntegrationTestFixture.CreateAsync();
        var resp = await fx.Client.GetAsync(new Uri("/consumer-400-probe", UriKind.Relative));
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        var pd = await resp.Content.ReadFromJsonAsync<ProblemDetails>();
        await Assert.That(pd).IsNotNull();
        // Consumer-set Type from a non-library domain MUST be preserved.
        await Assert.That(pd!.Type).IsEqualTo("https://consumer.example/errors/payment");
    }

    [Test]
    public async Task Non400_ProblemDetails_TypeNotOverwritten()
    {
        await using var fx = await IntegrationTestFixture.CreateAsync();
        var resp = await fx.Client.GetAsync(new Uri("/internal-error-probe", UriKind.Relative));
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.InternalServerError);
        var pd = await resp.Content.ReadFromJsonAsync<ProblemDetails>();
        await Assert.That(pd).IsNotNull();
        if (pd!.Type is not null)
        {
            await Assert.That(pd.Type.StartsWith("https://civitas-id.dev/errors/", StringComparison.Ordinal)).IsFalse();
        }
    }
}
