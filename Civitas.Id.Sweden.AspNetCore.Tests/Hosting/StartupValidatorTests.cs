using System.Text.Json.Serialization.Metadata;
using Civitas.Id.Sweden.AspNetCore.Extensions;
using Microsoft.AspNetCore.Http.Json;

namespace Civitas.Id.Sweden.AspNetCore.Tests.Hosting;

public sealed class StartupValidatorTests
{
    [Test]
    public async Task StartAsync_Throws_When_ProblemDetails_Missing_From_Resolver_Chain()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddCivitasIdSwedenAspNetCore();

        // Override Http.Json.JsonOptions with an empty resolver chain
        // (simulates misconfiguration). PostConfigure runs after the
        // library's IConfigureOptions, so this clears the chain the
        // library populated.
        builder.Services.PostConfigure<JsonOptions>(static opts =>
        {
            opts.SerializerOptions.TypeInfoResolverChain.Clear();
            opts.SerializerOptions.TypeInfoResolver = JsonTypeInfoResolver.Combine();
        });

        var app = builder.Build();

        var ex = await Assert.That(async () => await app.StartAsync())
            .Throws<InvalidOperationException>();
        await Assert.That(ex!.Message).Contains("ProblemDetails");

        await app.DisposeAsync();
    }

    [Test]
    public async Task StartAsync_Succeeds_When_ProblemDetails_Reachable()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddCivitasIdSwedenAspNetCore();

        var app = builder.Build();
        await app.StartAsync();
        await app.StopAsync();
        await app.DisposeAsync();
    }
}
