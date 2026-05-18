using System.Text.Json;
using Civitas.Id.Sweden.AspNetCore.Extensions;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Format;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace Civitas.Id.Sweden.AspNetCore.Tests.Json;

public sealed class FormatAwareConverterTests
{
    [Test]
    public async Task ShortFormat_Option_Produces_ShortFormat_Wire_Output()
    {
        var services = new ServiceCollection();
        services.AddCivitasIdSwedenAspNetCore(o => o.JsonFormat = PnrFormat.ShortFormat);
        var sp = services.BuildServiceProvider();
        var opts = sp.GetRequiredService<IOptions<JsonOptions>>().Value;
        var personalId = PersonalId.Parse("198112189876", DateOnly.FromDateTime(DateTime.UtcNow));
        var json = JsonSerializer.Serialize(personalId, opts.SerializerOptions);
        await Assert.That(json).IsEqualTo("\"8112189876\"");
    }

    [Test]
    public async Task DefaultConfig_NoJsonFormatSet_Produces_LongFormat()
    {
        var services = new ServiceCollection();
        services.AddCivitasIdSwedenAspNetCore();
        var sp = services.BuildServiceProvider();
        var opts = sp.GetRequiredService<IOptions<JsonOptions>>().Value;
        var personalId = PersonalId.Parse("198112189876");
        var json = JsonSerializer.Serialize(personalId, opts.SerializerOptions);
        await Assert.That(json).IsEqualTo("\"198112189876\"");
    }

    [Test]
    public async Task ShortFormat_Deserializes_12Digit_Input()
    {
        var services = new ServiceCollection();
        services.AddCivitasIdSwedenAspNetCore(o => o.JsonFormat = PnrFormat.ShortFormat);
        var sp = services.BuildServiceProvider();
        var opts = sp.GetRequiredService<IOptions<JsonOptions>>().Value;
        var parsed = JsonSerializer.Deserialize<PersonalId>("\"198112189876\"", opts.SerializerOptions);
        await Assert.That(parsed).IsNotNull();
        await Assert.That(parsed!.LongFormat()).IsEqualTo("198112189876");
    }

    [Test]
    public async Task LongFormat_RoundTrip_AllFourTypes()
    {
        var services = new ServiceCollection();
        services.AddCivitasIdSwedenAspNetCore();
        var sp = services.BuildServiceProvider();
        var opts = sp.GetRequiredService<IOptions<JsonOptions>>().Value;

        // 198112189876 = PersonalId; 191401682396 = CoordinationId; 5560160680 = OrganisationId (Aktiebolag).
        foreach (var raw in new[] { "198112189876", "191401682396", "5560160680" })
        {
            var parsed = SwedishOfficialId.ParseAny(raw);
            var json = JsonSerializer.Serialize(parsed, parsed.GetType(), opts.SerializerOptions);
            var back = (SwedishOfficialId)JsonSerializer.Deserialize(json, parsed.GetType(), opts.SerializerOptions)!;
            await Assert.That(back).IsEqualTo(parsed);
        }
    }
}
