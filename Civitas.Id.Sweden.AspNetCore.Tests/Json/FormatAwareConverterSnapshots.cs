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

    [Test]
    public async Task ShortFormat_Centenarian_NoSeparator()
    {
        // Note: AspNetCore options validator (Phase 3) rejects ShortFormatWithSeparator,
        // so the centenarian "+" sentinel never reaches the wire through the library's
        // JSON pipeline. This snapshot pins the hyphenless ShortFormat output for
        // centenarians. If a future v1.x widens the validator to permit
        // ShortFormatWithSeparator, this snapshot MUST be updated to include "+".
        // Until then, the absence of "+" is the locked-in expected behavior.
        //
        // Skatteverket fixture 189001019802: born 1890-01-01, age >100 -> centenarian.
        var pid = PersonalId.Parse("189001019802", new DateOnly(2026, 5, 19));
        var json = SerializeWith(PnrFormat.ShortFormat, pid);
        await Verify(json, "json")
            .UseFileName("ShortFormat_Centenarian_NoSeparator");
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
