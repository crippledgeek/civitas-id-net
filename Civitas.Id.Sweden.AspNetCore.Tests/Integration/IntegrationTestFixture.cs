using Civitas.Id.Sweden.AspNetCore.Endpoints;
using Civitas.Id.Sweden.AspNetCore.Extensions;
using Civitas.Id.Sweden.AspNetCore.OpenApi;
using Civitas.Id.Sweden.AspNetCore.Tests.Integration.TestApp;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Format;
using Microsoft.AspNetCore.Mvc;
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

        // OpenAPI is opt-in as of Phase 2 (Finding 3): library no longer
        // calls AddOpenApi from RegisterInfrastructure.
        builder.Services.AddOpenApi(opts => opts.AddCivitasIdSwedenSchemas());

        var app = builder.Build();
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.MapOpenApi();

        var typed = app.MapGroup(string.Empty).WithCivitasIdSwedenMetadata();

        _ = typed.MapGet("/customers/{id}", (PersonalId id) =>
            Results.Ok(new CustomerResponse(id, id.GetAge(), id.IsAdult())));

        _ = typed.MapGet("/lookup", (PersonalId id) => Results.Ok(new LookupResponse(id)));

        // Body deserialization of a top-level PersonalId works through the chained
        // TypeInfoResolver. Wrapping in a DTO with a typed property relies on
        // STJ resolving converter-per-property; that pathway requires either
        // [JsonConverter] on the type or converter registration on the
        // serializer options. The integration tests cover the top-level body
        // path, which is what the AspNetCore extension actually wires up.
        _ = typed.MapPost("/customers", ([FromBody] PersonalId id) =>
            Results.Created($"/customers/{id}", new LookupResponse(id)));

        // Polymorphic SwedishOfficialId: parse manually inside the handler since
        // SwedishOfficialId does not implement IParsable<T> (sum-type base).
        _ = typed.MapGet("/any/{id}", (string id) =>
        {
            var parsed = SwedishOfficialId.ParseAny(id);
            return Results.Ok(new AnyResponse(parsed.GetType().Name, parsed));
        });

        _ = typed.MapGet("/orgs/{id}", (OrganisationId id) => Results.Ok(new OrgResponse(
            id,
            id.Form,
            id.NumberType == OrganisationNumberType.PhysicalPerson)));

        // Forces the explicit throw -> exception-handler path. Used to verify
        // ProblemDetails body emitted by InvalidIdNumberExceptionHandler.
        _ = typed.MapGet("/customers-strict/{idString}", (string idString) =>
        {
            var id = PersonalId.Parse(idString);
            return Results.Ok(new LookupResponse(id));
        });

        // Org strict variant — same role as /customers-strict but for OrganisationId.
        _ = typed.MapGet("/orgs-strict/{idString}", (string idString) =>
        {
            var id = OrganisationId.Parse(idString);
            return Results.Ok(new OrgResponse(id, id.Form, id.NumberType == OrganisationNumberType.PhysicalPerson));
        });

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
