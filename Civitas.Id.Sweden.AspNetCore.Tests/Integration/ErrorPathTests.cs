using System.Net;
using System.Text.Json.Nodes;

namespace Civitas.Id.Sweden.AspNetCore.Tests.Integration;

public class ErrorPathTests
{
    public class MalformedRouteParameter
    {
        [Test]
        public async Task Returns400_ForUnparseableTypedRouteValue()
        {
            await using var fx = await IntegrationTestFixture.CreateAsync();
            var res = await fx.Client.GetAsync(new Uri("/customers/19811218bad9", UriKind.Relative));
            await Assert.That(res.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        }

        [Test]
        public async Task Returns400_ForInvalidChecksum()
        {
            await using var fx = await IntegrationTestFixture.CreateAsync();
            var res = await fx.Client.GetAsync(new Uri("/customers/198112189870", UriKind.Relative));
            await Assert.That(res.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        }
    }

    public class ExplicitParseFailure
    {
        [Test]
        public async Task ExceptionHandler_Returns_ProblemDetailsContentType()
        {
            await using var fx = await IntegrationTestFixture.CreateAsync();
            var res = await fx.Client.GetAsync(new Uri("/customers-strict/19811218bad9", UriKind.Relative));

            await Assert.That(res.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
            await Assert.That(res.Content.Headers.ContentType?.MediaType)
                .IsEqualTo("application/problem+json");
        }

        [Test]
        public async Task ExceptionHandler_BodyContainsReasonAndRedactedInput()
        {
            await using var fx = await IntegrationTestFixture.CreateAsync();
            var res = await fx.Client.GetAsync(new Uri("/customers-strict/19811218bad9", UriKind.Relative));

            await Assert.That(res.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);

            var body = await res.Content.ReadFromJsonAsync<JsonNode>();
            await Assert.That(body).IsNotNull();
            await Assert.That(body!["title"]?.ToString()).IsEqualTo("Invalid Swedish ID number");
            await Assert.That(body["status"]?.GetValue<int>()).IsEqualTo(400);
            await Assert.That(body["reason"]).IsNotNull();
            await Assert.That(body["input"]).IsNotNull();

            // RedactInput delegate masks all but the last 4 chars.
            var redacted = body["input"]?.ToString();
            await Assert.That(redacted).IsNotNull();
            await Assert.That(redacted!.EndsWith("bad9", StringComparison.Ordinal)).IsTrue();
            await Assert.That(redacted.StartsWith('*')).IsTrue();
        }

        [Test]
        public async Task ExceptionHandler_RedactedInputDoesNotLeakRawInput()
        {
            await using var fx = await IntegrationTestFixture.CreateAsync();
            var res = await fx.Client.GetAsync(new Uri("/customers-strict/19811218bad9", UriKind.Relative));

            var body = await res.Content.ReadFromJsonAsync<JsonNode>();
            var redacted = body?["input"]?.ToString();

            await Assert.That(redacted).IsNotNull();
            await Assert.That(redacted!.Contains("19811218", StringComparison.Ordinal)).IsFalse();
        }
    }

    public class InvalidJsonBody
    {
        [Test]
        public async Task MalformedPersonalIdBody_Returns400()
        {
            await using var fx = await IntegrationTestFixture.CreateAsync();
            using var content = new StringContent(
                "\"19811218bad9\"",
                System.Text.Encoding.UTF8,
                "application/json");
            var res = await fx.Client.PostAsync(new Uri("/customers", UriKind.Relative), content);

            await Assert.That(res.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        }
    }
}
