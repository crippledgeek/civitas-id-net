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
    public async Task ShortFormat_Centenarian_Wire_Output()
    {
        // Skatteverket fixture 189001019802: born 1890-01-01, age >100 -> centenarian.
        //
        // The AspNetCore JsonFormat option accepts only PnrFormat.LongFormat or
        // PnrFormat.ShortFormat (ShortFormatWithSeparator is rejected by the
        // options validator). PnrFormat.ShortFormat produces a raw 10-digit form
        // with no hyphen/plus separator. The centenarian "+" sentinel only
        // appears in ShortFormatWithSeparator, which is not reachable through
        // the AspNetCore converters.
        //
        // This snapshot locks the empirical wire output emitted by the
        // PersonalIdShortFormatJsonConverter for a centenarian PersonalId.
        // If the converter is later upgraded to emit ShortFormatWithSeparator
        // (and the JsonFormat validator widened to accept it), update the
        // verified snapshot.
        var pid = PersonalId.Parse("189001019802", new DateOnly(2026, 5, 19));
        var json = SerializeWith(PnrFormat.ShortFormat, pid);
        await Verify(json, "json")
            .UseFileName("ShortFormat_Centenarian_Plus");
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
