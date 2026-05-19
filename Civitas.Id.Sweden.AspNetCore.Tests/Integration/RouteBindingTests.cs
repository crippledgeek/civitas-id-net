using System.Net;
using System.Text.Json.Nodes;

namespace Civitas.Id.Sweden.AspNetCore.Tests.Integration;

public class RouteBindingTests
{
    public class TypedRouteParameter
    {
        [Test]
        public async Task ValidPersonalId_Returns200()
        {
            await using var fx = await IntegrationTestFixture.CreateAsync();
            var res = await fx.Client.GetAsync(new Uri("/customers/198112189876", UriKind.Relative));
            await Assert.That(res.StatusCode).IsEqualTo(HttpStatusCode.OK);
        }

        [Test]
        public async Task ValidOrganisationId_Returns200_LegalPerson()
        {
            await using var fx = await IntegrationTestFixture.CreateAsync();
            var res = await fx.Client.GetAsync(new Uri("/orgs/5560160680", UriKind.Relative));
            await Assert.That(res.StatusCode).IsEqualTo(HttpStatusCode.OK);

            var body = await res.Content.ReadFromJsonAsync<JsonNode>();
            await Assert.That(body).IsNotNull();
            await Assert.That(body!["isPhysical"]?.GetValue<bool>()).IsFalse();
        }

        [Test]
        public async Task PolymorphicId_DispatchesToPersonalId()
        {
            await using var fx = await IntegrationTestFixture.CreateAsync();
            var res = await fx.Client.GetAsync(new Uri("/any/198112189876", UriKind.Relative));
            await Assert.That(res.StatusCode).IsEqualTo(HttpStatusCode.OK);

            var body = await res.Content.ReadFromJsonAsync<JsonNode>();
            await Assert.That(body).IsNotNull();
            await Assert.That(body!["type"]?.ToString()).IsEqualTo("PersonalId");
        }

        [Test]
        public async Task PolymorphicId_DispatchesToOrganisationId()
        {
            await using var fx = await IntegrationTestFixture.CreateAsync();
            var res = await fx.Client.GetAsync(new Uri("/any/5560160680", UriKind.Relative));
            await Assert.That(res.StatusCode).IsEqualTo(HttpStatusCode.OK);

            var body = await res.Content.ReadFromJsonAsync<JsonNode>();
            await Assert.That(body).IsNotNull();
            await Assert.That(body!["type"]?.ToString()).IsEqualTo("OrganisationId");
        }
    }

    public class TypedQueryParameter
    {
        [Test]
        public async Task ValidPersonalId_Returns200()
        {
            await using var fx = await IntegrationTestFixture.CreateAsync();
            var res = await fx.Client.GetAsync(new Uri("/lookup?id=198112189876", UriKind.Relative));
            await Assert.That(res.StatusCode).IsEqualTo(HttpStatusCode.OK);
        }
    }

    public class TypedJsonBody
    {
        [Test]
        public async Task ValidPersonalIdAsBody_Returns201()
        {
            await using var fx = await IntegrationTestFixture.CreateAsync();

            // Top-level PersonalId in body — exercises CivitasIdSwedenJsonContext
            // via the chained TypeInfoResolver registered by AddCivitasIdSwedenAspNetCore.
            using var content = new StringContent(
                "\"198112189876\"",
                System.Text.Encoding.UTF8,
                "application/json");
            var res = await fx.Client.PostAsync(new Uri("/customers", UriKind.Relative), content);

            await Assert.That(res.StatusCode).IsEqualTo(HttpStatusCode.Created);
        }

        [Test]
        public async Task RoundTripsTypedIdInResponseBody()
        {
            await using var fx = await IntegrationTestFixture.CreateAsync();
            using var content = new StringContent(
                "\"198112189876\"",
                System.Text.Encoding.UTF8,
                "application/json");
            var res = await fx.Client.PostAsync(new Uri("/customers", UriKind.Relative), content);

            await Assert.That(res.StatusCode).IsEqualTo(HttpStatusCode.Created);

            var body = await res.Content.ReadFromJsonAsync<JsonNode>();
            await Assert.That(body).IsNotNull();
            await Assert.That(body!["id"]?.ToString()).IsEqualTo("198112189876");
        }
    }
}
