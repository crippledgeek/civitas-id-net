using Civitas.Id.Sweden.AspNetCore.Endpoints;
using Civitas.Id.Sweden.AspNetCore.Extensions;
using Civitas.Id.Sweden.AspNetCore.OpenApi;
using Civitas.Id.Sweden.AspNetCore.Tests.Integration.TestApp;
using Civitas.Id.Sweden.Format;
using Microsoft.AspNetCore.TestHost;

namespace Civitas.Id.Sweden.AspNetCore.Tests.Integration;

/// <summary>
/// In-memory host fixture for end-to-end ASP.NET Core integration tests.
/// Uses <see cref="TestServer"/> via <c>WebHost.UseTestServer()</c>, avoiding
/// entry-point conflicts with the TUnit Microsoft.Testing.Platform host.
/// </summary>
internal sealed class IntegrationTestFixture : IAsyncDisposable
{
    private readonly WebApplication _app;

    public HttpClient Client { get; }

    private IntegrationTestFixture(WebApplication app, HttpClient client)
    {
        _app = app;
        Client = client;
    }

    public static async Task<IntegrationTestFixture> CreateAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddCivitasIdSwedenAspNetCore(static o =>
        {
            o.JsonFormat = PnrFormat.LongFormat;
            o.RedactInput = static s => s.Length >= 4
                ? new string('*', s.Length - 4) + s[^4..]
                : "****";
        });

        // MVC controllers — exercises [ApiController] auto-400 path used by
        // CustomizeProblemDetailsTests + ValidationProblemDetails locking.
        builder.Services.AddControllers().AddCivitasIdSweden()
            .AddApplicationPart(typeof(IntegrationTestFixture).Assembly);

        // OpenAPI is opt-in as of Phase 2 (Finding 3): library no longer
        // calls AddOpenApi from RegisterInfrastructure.
        builder.Services.AddOpenApi(opts => opts.AddCivitasIdSwedenSchemas());

        var app = builder.Build();
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.MapOpenApi();
        app.MapControllers();

        // Endpoint registration is centralized in TestEndpointsBuilder so the
        // fixture body stays focused on host wiring (DI, middleware, lifetime)
        // and the test-endpoint surface can grow without making the fixture
        // illegible. See TestEndpointsBuilder for endpoint → test-class
        // ownership documentation.
        _ = app.MapGroup(string.Empty)
            .WithCivitasIdSwedenMetadata()
            .MapTestEndpoints();

        await app.StartAsync();
        return new IntegrationTestFixture(app, app.GetTestClient());
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}
