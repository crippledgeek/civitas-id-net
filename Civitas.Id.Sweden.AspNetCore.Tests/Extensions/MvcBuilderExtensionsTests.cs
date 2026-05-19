using Civitas.Id.Sweden.AspNetCore.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Civitas.Id.Sweden.AspNetCore.Tests.Extensions;

/// <summary>
/// Locks the <see cref="IMvcBuilder"/> integration (Gap C). The
/// <see cref="ApiBehaviorOptions.ClientErrorMapping"/>[400] entry MUST carry
/// the library's normalized <c>type</c> URI so the <c>[ApiController]</c>
/// auto-400 path emits a ValidationProblemDetails with the correct link.
/// </summary>
public sealed class MvcBuilderExtensionsTests
{
    [Test]
    public async Task AddCivitasIdSweden_OnIMvcBuilder_Sets_ClientErrorMapping_400()
    {
        var services = new ServiceCollection();
        services.AddCivitasIdSwedenAspNetCore();
        services.AddControllers().AddCivitasIdSweden();
        await using var sp = services.BuildServiceProvider();
        var apiOpts = sp.GetRequiredService<IOptions<ApiBehaviorOptions>>().Value;

        await Assert.That(apiOpts.ClientErrorMapping[StatusCodes.Status400BadRequest].Link).IsNotNull();
        await Assert.That(apiOpts.ClientErrorMapping[StatusCodes.Status400BadRequest].Link!)
            .StartsWith("https://civitas-id.dev/errors/");
    }

    [Test]
    public async Task AddCivitasIdSweden_CalledTwice_DoesNotDuplicate()
    {
        var services = new ServiceCollection();
        services.AddCivitasIdSwedenAspNetCore();
        services.AddControllers().AddCivitasIdSweden().AddCivitasIdSweden();
        await using var sp = services.BuildServiceProvider();
        var apiOpts = sp.GetRequiredService<IOptions<ApiBehaviorOptions>>().Value;

        // Only one ClientErrorData per 400 — last-write-wins, not duplicated.
        await Assert.That(apiOpts.ClientErrorMapping[StatusCodes.Status400BadRequest].Link).IsNotNull();
        await Assert.That(apiOpts.ClientErrorMapping[StatusCodes.Status400BadRequest].Link!)
            .StartsWith("https://civitas-id.dev/errors/");
    }

    [Test]
    public async Task Consumer_ClientErrorMapping_After_Library_Wins()
    {
        var services = new ServiceCollection();
        services.AddCivitasIdSwedenAspNetCore();
        services.AddControllers().AddCivitasIdSweden();
        services.Configure<ApiBehaviorOptions>(static o => o.ClientErrorMapping[StatusCodes.Status400BadRequest] =
            new ClientErrorData
            {
                Title = "Consumer override",
                Link = "https://consumer.example/errors/400"
            });
        await using var sp = services.BuildServiceProvider();
        var apiOpts = sp.GetRequiredService<IOptions<ApiBehaviorOptions>>().Value;

        await Assert.That(apiOpts.ClientErrorMapping[StatusCodes.Status400BadRequest].Link)
            .IsEqualTo("https://consumer.example/errors/400");
    }

    [Test]
    public async Task AddCivitasIdSweden_NullBuilder_Throws()
    {
        IMvcBuilder builder = null!;
        await Assert.That(Act).Throws<ArgumentNullException>();
        return;

        void Act() => _ = builder.AddCivitasIdSweden();
    }
}
