using System.Text.Json;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Json.Converters;

namespace Civitas.Id.Sweden.Json.Tests;

public class CivitasIdSwedenJsonContextTests
{
    [Test]
    public async Task Context_KnowsAllFourTypes()
    {
        var ctx = CivitasIdSwedenJsonContext.Default;
        await Assert.That(ctx.GetTypeInfo(typeof(PersonalId))).IsNotNull();
        await Assert.That(ctx.GetTypeInfo(typeof(CoordinationId))).IsNotNull();
        await Assert.That(ctx.GetTypeInfo(typeof(OrganisationId))).IsNotNull();
        await Assert.That(ctx.GetTypeInfo(typeof(SwedishOfficialId))).IsNotNull();
    }

    [Test]
    public async Task ChainedContext_AllowsConverterRoundTrip()
    {
        // The source-gen context provides metadata; consumers add the
        // JsonConverter<T> to handle the canonical-string format.
        var opts = new JsonSerializerOptions
        {
            Converters =
            {
                new PersonalIdJsonConverter(),
                new CoordinationIdJsonConverter(),
                new OrganisationIdJsonConverter(),
                new SwedishOfficialIdJsonConverter()
            }
        };
        opts.TypeInfoResolverChain.Add(CivitasIdSwedenJsonContext.Default);

        var id = PersonalId.Parse("189001019802");
        var json = JsonSerializer.Serialize(id, opts);
        var round = JsonSerializer.Deserialize<PersonalId>(json, opts);
        await Assert.That(round).IsEqualTo(id);
    }
}
