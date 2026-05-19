using System.Net.Http.Headers;
using System.Text;
using Civitas.Id.Sweden.AspNetCore.Extensions;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Format;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Options;

namespace Civitas.Id.Sweden.AspNetCore.Tests.Integration;

/// <summary>
/// Verifies that the Mvc.JsonOptions bridge (Gap A) wires Civitas converters
/// into the MVC SystemTextJsonInputFormatter / SystemTextJsonOutputFormatter
/// pipeline used by <c>[FromBody]</c> on controllers.
/// </summary>
public sealed class MvcJsonOptionsBridgeTests
{
    private static StringContent JsonContent(string body)
    {
        var c = new StringContent(body, Encoding.UTF8);
        c.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return c;
    }

    [Test]
    public async Task MvcController_FromBody_RoundTrips_PersonalId()
    {
        using var host = await new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(s =>
                {
                    s.AddCivitasIdSwedenAspNetCore();
                    s.AddControllers().AddApplicationPart(typeof(MvcJsonOptionsBridgeTests).Assembly);
                })
                .Configure(app => app
                    .UseRouting()
                    .UseEndpoints(e => e.MapControllers())))
            .StartAsync();
        var client = host.GetTestClient();

        // Wire shape produced/consumed by the Civitas long-format string converter.
        const string body = """{"id":"198112189876"}""";
        using var content = JsonContent(body);
        var resp = await client.PostAsync(new Uri("/mvc-bridge-test", UriKind.Relative), content);
        resp.EnsureSuccessStatusCode();
        var echoed = await resp.Content.ReadAsStringAsync();

        // Server-side: SystemTextJsonInputFormatter MUST use the Civitas converter
        // to deserialize the string "198112189876" into a PersonalId — without the
        // Mvc.JsonOptions bridge it would have failed with 400 model-binding error.
        // Server-side output formatter MUST round-trip the same wire shape.
        await Assert.That(echoed).Contains("198112189876");
    }

    [Test]
    public async Task RegisteringMvcJsonOptions_WithoutAddControllers_DoesNotThrow()
    {
        using var host = await new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(s => s.AddCivitasIdSwedenAspNetCore())
                .Configure(_ => { }))
            .StartAsync();
        // Reaching here without exception confirms the silent no-op.
        await Assert.That(host).IsNotNull();

        // Probe Mvc.JsonOptions resolution behaviour when AddControllers() is
        // never called. The IConfigureOptions registration via TryAddEnumerable
        // is harmless: it executes only if Mvc.JsonOptions is actively resolved.
        // The relevant invariant is that resolving the options object does not
        // throw and yields a JsonSerializerOptions instance.
        var mvcOpts = host.Services.GetService<IOptions<Microsoft.AspNetCore.Mvc.JsonOptions>>();
        if (mvcOpts is not null)
        {
            await Assert.That(mvcOpts.Value.JsonSerializerOptions).IsNotNull();
        }
    }

    [Test]
    public async Task ShortFormat_AppliesAcrossBothSurfaces()
    {
        using var host = await new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(s =>
                {
                    s.AddCivitasIdSwedenAspNetCore(o => o.JsonFormat = PnrFormat.ShortFormat);
                    s.AddControllers().AddApplicationPart(typeof(MvcJsonOptionsBridgeTests).Assembly);
                })
                .Configure(app => app.UseRouting().UseEndpoints(e =>
                {
                    e.MapControllers();
                    e.MapGet("/minimal-bridge-test", () => PersonalId.Parse("198112189876"));
                })))
            .StartAsync();
        var client = host.GetTestClient();

        // MVC body deserializes 12-digit input and re-emits ShortFormat (10-digit).
        const string requestBody = """{"id":"198112189876"}""";
        using var content = JsonContent(requestBody);
        var mvcResp = await client.PostAsync(new Uri("/mvc-bridge-test", UriKind.Relative), content);
        mvcResp.EnsureSuccessStatusCode();
        var mvcBody = await mvcResp.Content.ReadAsStringAsync();
        await Assert.That(mvcBody).Contains("8112189876");

        // Minimal API emits same ShortFormat — proves the SAME PnrFormat option drives both surfaces.
        var minimalBody = await client.GetStringAsync(new Uri("/minimal-bridge-test", UriKind.Relative));
        await Assert.That(minimalBody).IsEqualTo("\"8112189876\"");
    }

    /// <summary>DTO used by <see cref="MvcBridgeTestController"/> to exercise the [FromBody] formatter path.</summary>
    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Materialized by System.Text.Json.")]
    public sealed record TestDto([property: SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global", Justification = "Accessed via JSON serializer.")] PersonalId Id);
}

/// <summary>
/// Top-level controller for the MVC bridge test. ASP.NET Core's default
/// <c>ControllerFeatureProvider</c> excludes nested types — keep this at
/// namespace scope so <c>AddControllers().AddApplicationPart(...)</c>
/// discovers it.
/// </summary>
[Route("/mvc-bridge-test")]
[ApiController]
public sealed class MvcBridgeTestController : ControllerBase
{
    /// <summary>Echoes the posted DTO so the round-trip can be asserted.</summary>
    [HttpPost]
    public ActionResult<MvcJsonOptionsBridgeTests.TestDto> Echo([FromBody] MvcJsonOptionsBridgeTests.TestDto dto) => Ok(dto);
}
