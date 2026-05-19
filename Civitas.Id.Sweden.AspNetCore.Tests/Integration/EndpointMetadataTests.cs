using System.Text.Json.Nodes;

namespace Civitas.Id.Sweden.AspNetCore.Tests.Integration;

public class EndpointMetadataTests
{
    [Test]
    public async Task ProtectedEndpoints_Advertise400Response()
    {
        await using var fx = await IntegrationTestFixture.CreateAsync();
        var doc = await fx.Client.GetFromJsonAsync<JsonNode>(
            new Uri("/openapi/v1.json", UriKind.Relative));

        var customerGet = doc?["paths"]?["/customers/{id}"]?["get"];
        await Assert.That(customerGet).IsNotNull();

        var responses = customerGet!["responses"];
        await Assert.That(responses).IsNotNull();
        await Assert.That(responses!["400"]).IsNotNull();
    }

    [Test]
    public async Task ProtectedEndpoints_400Response_AdvertisesProblemJsonContentType()
    {
        await using var fx = await IntegrationTestFixture.CreateAsync();
        var doc = await fx.Client.GetFromJsonAsync<JsonNode>(
            new Uri("/openapi/v1.json", UriKind.Relative));

        var customerGet = doc?["paths"]?["/customers/{id}"]?["get"];
        var problemContent = customerGet?["responses"]?["400"]?["content"]?["application/problem+json"];

        await Assert.That(problemContent).IsNotNull();
    }
}
