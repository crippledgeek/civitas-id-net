using Civitas.Id.Sweden.AspNetCore.Endpoints;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace Civitas.Id.Sweden.AspNetCore.Tests.Endpoints;

public class CivitasIdSwedenEndpointConventionExtensionsTests
{
    public class WithCivitasIdSwedenMetadata
    {
        [Test]
        public async Task Adds400ProblemDetailsMetadata()
        {
            var builder = WebApplication.CreateBuilder();
            await using var app = builder.Build();

            _ = app.MapGet("/test/{id}", (string id) => Results.Ok(id))
                .WithCivitasIdSwedenMetadata();

            var dataSource = ((IEndpointRouteBuilder)app).DataSources.First();
            var endpoint = dataSource.Endpoints[0];
            var produces = endpoint.Metadata.GetMetadata<IProducesResponseTypeMetadata>();

            await Assert.That(produces).IsNotNull();
            await Assert.That(produces!.StatusCode).IsEqualTo(StatusCodes.Status400BadRequest);
            await Assert.That(produces.Type).IsEqualTo(typeof(ProblemDetails));
            await Assert.That(produces.ContentTypes.Contains("application/problem+json")).IsTrue();
        }

        [Test]
        public async Task ReturnsSameBuilder_ForChaining()
        {
            var builder = WebApplication.CreateBuilder();
            await using var app = builder.Build();

            var route = app.MapGet("/x", () => Results.Ok());
            var result = route.WithCivitasIdSwedenMetadata();

            await Assert.That(ReferenceEquals(route, result)).IsTrue();
        }

        [Test]
        public async Task NullBuilder_Throws()
        {
            RouteGroupBuilder? nullBuilder = null;
            await Assert.That(() => nullBuilder!.WithCivitasIdSwedenMetadata())
                .Throws<ArgumentNullException>();
        }
    }
}
