using System.Net;
using System.Text.Json.Nodes;

namespace Civitas.Id.Sweden.AspNetCore.Tests.Integration;

public class OpenApiDocumentTests
{
    [Test]
    public async Task DocumentIsAvailable()
    {
        await using var fx = await IntegrationTestFixture.CreateAsync();
        var res = await fx.Client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));
        await Assert.That(res.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task DocumentIsValidJson_WithOpenApiVersionField()
    {
        await using var fx = await IntegrationTestFixture.CreateAsync();
        var doc = await fx.Client.GetFromJsonAsync<JsonNode>(new Uri("/openapi/v1.json", UriKind.Relative));

        await Assert.That(doc).IsNotNull();
        await Assert.That(doc!["openapi"]).IsNotNull();
    }

    [Test]
    public async Task DocumentEnumerates_MappedTypedRoutes()
    {
        await using var fx = await IntegrationTestFixture.CreateAsync();
        var doc = await fx.Client.GetFromJsonAsync<JsonNode>(new Uri("/openapi/v1.json", UriKind.Relative));

        var paths = doc?["paths"];
        await Assert.That(paths).IsNotNull();
        await Assert.That(paths!["/customers/{id}"]).IsNotNull();
        await Assert.That(paths["/lookup"]).IsNotNull();
        await Assert.That(paths["/orgs/{id}"]).IsNotNull();
    }
}
