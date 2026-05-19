using System.Text.Json;
using Civitas.Id.Sweden.AspNetCore.Extensions;
using Civitas.Id.Sweden.AspNetCore.OpenApi;
using Civitas.Id.Sweden.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Options;

namespace Civitas.Id.Sweden.AspNetCore.Tests.Extensions;

public sealed class OpenApiOptInTests
{
    [Test]
    public async Task AddCivitasIdSwedenAspNetCore_Alone_DoesNotRegisterOpenApi()
    {
        var services = new ServiceCollection();
        services.AddCivitasIdSwedenAspNetCore();

        // AddOpenApi() registers IConfigureOptions<OpenApiOptions> entries
        // that wire up the document name and schema transformers. If the
        // library has stopped calling AddOpenApi(), the consumer's service
        // collection contains NO such registrations.
        // Pre-fix: non-zero (RegisterInfrastructure called AddOpenApi).
        // Post-fix: zero (opt-in only).
        var openApiConfigurations = services
            .Where(static d => d.ServiceType == typeof(IConfigureOptions<OpenApiOptions>))
            .ToArray();

        await Assert.That(openApiConfigurations).IsEmpty();
    }

    [Test]
    public async Task AddCivitasIdSwedenSchemas_AppliesTransformerToEmittedDoc()
    {
        using var host = await new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(s =>
                {
                    s.AddCivitasIdSwedenAspNetCore();
                    s.AddOpenApi(opts => opts.AddCivitasIdSwedenSchemas());
                    s.AddRouting();
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(e =>
                    {
                        e.MapOpenApi();
                        // Use [FromBody] so PersonalId emits a body schema
                        // (this is the path the transformer is meant to enrich).
                        e.MapPost("/probe", ([FromBody] PersonalId id) => Results.Ok(id));
                    });
                }))
            .StartAsync();
        var client = host.GetTestClient();

        var doc = await client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");

        // The request body schema for PersonalId is a $ref into components.schemas.
        // The transformer mutates the schema referenced by that $ref.
        var requestBodySchemaRef = doc
            .GetProperty("paths").GetProperty("/probe").GetProperty("post")
            .GetProperty("requestBody").GetProperty("content")
            .GetProperty("application/json").GetProperty("schema").GetProperty("$ref")
            .GetString();

        await Assert.That(requestBodySchemaRef).IsEqualTo("#/components/schemas/PersonalId");

        var personalIdSchema = doc.GetProperty("components")
            .GetProperty("schemas").GetProperty("PersonalId");

        await Assert.That(personalIdSchema.GetProperty("format").GetString()).IsEqualTo("personnummer");
    }
}
