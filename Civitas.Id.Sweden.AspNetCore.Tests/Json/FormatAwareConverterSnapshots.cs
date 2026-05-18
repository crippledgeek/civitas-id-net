using System.Text.Json;
using Civitas.Id.Sweden.AspNetCore.Extensions;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Format;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace Civitas.Id.Sweden.AspNetCore.Tests.Json;

// Verify.TUnit auto-initializes via [ModuleInitializer]; no [UsesVerify] needed.
public sealed class FormatAwareConverterSnapshots
{
    [Test]
    public async Task LongFormat_PersonalId()
    {
        var json = SerializeWith(PnrFormat.LongFormat, PersonalId.Parse("198112189876"));
        await Verify(json).UseFileName("LongFormat_PersonalId");
    }

    [Test]
    public async Task ShortFormat_PersonalId()
    {
        var json = SerializeWith(PnrFormat.ShortFormat, PersonalId.Parse("198112189876"));
        await Verify(json).UseFileName("ShortFormat_PersonalId");
    }

    private static string SerializeWith<T>(PnrFormat format, T value) where T : notnull
    {
        var services = new ServiceCollection();
        services.AddCivitasIdSwedenAspNetCore(o => o.JsonFormat = format);
        var sp = services.BuildServiceProvider();
        var opts = sp.GetRequiredService<IOptions<JsonOptions>>().Value;
        return JsonSerializer.Serialize(value, opts.SerializerOptions);
    }
}
