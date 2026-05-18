using Civitas.Id.Sweden.AspNetCore.Extensions;
using Civitas.Id.Sweden.AspNetCore.Options;
using Microsoft.Extensions.Options;

namespace Civitas.Id.Sweden.AspNetCore.Tests.Options;

public sealed class ProblemDetailsTypeBaseUriTests
{
    [Test]
    public async Task Default_ProblemDetailsTypeBaseUri_EndsWithSlash()
    {
        var services = new ServiceCollection();
        services.AddCivitasIdSwedenAspNetCore();
        var sp = services.BuildServiceProvider();
        var opts = sp.GetRequiredService<IOptions<CivitasIdSwedenAspNetCoreOptions>>().Value;
        await Assert.That(opts.ProblemDetailsTypeBaseUri).IsEqualTo("https://civitas-id.dev/errors/");
    }

    [Test]
    public async Task NonSlashTerminated_Uri_FailsValidation()
    {
        var services = new ServiceCollection();
        services.AddCivitasIdSwedenAspNetCore(o => o.ProblemDetailsTypeBaseUri = "https://example.com/errors");
        await Assert.That(() =>
        {
            var sp = services.BuildServiceProvider();
            _ = sp.GetRequiredService<IOptions<CivitasIdSwedenAspNetCoreOptions>>().Value;
        }).Throws<OptionsValidationException>();
    }
}
