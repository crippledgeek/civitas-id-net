using System.Diagnostics.CodeAnalysis;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Tests.Helpers;
using Microsoft.Extensions.Configuration;

namespace Civitas.Id.Sweden.Tests.TypeConverters;

public class ConfigurationIntegrationTests
{
    // Properties bound by the IConfiguration reflection binder via TypeDescriptor
    // — set accessors are invoked by the framework, not directly from test code.
    private sealed class TestOptions
    {
        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local",
            Justification = "Set by IConfiguration reflection binder.")]
        public PersonalId? PersonId { get; set; }

        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local",
            Justification = "Set by IConfiguration reflection binder.")]
        public OrganisationId? OrgId { get; set; }
    }

    [Test]
    public async Task Bind_PersonalId_FromJsonConfig_RoundTrips()
    {
        var json = $$"""{"PersonId": "{{OfficialNumbers.Modern.Long}}", "OrgId": "{{LegalPersonNumbers.Aktiebolag.Digits}}"}""";
        var config = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)))
            .Build();

        var opts = new TestOptions();
        config.Bind(opts);

        await Assert.That(opts.PersonId).IsNotNull();
        await Assert.That(opts.PersonId!.LongFormat()).IsEqualTo(OfficialNumbers.Modern.Long);
        await Assert.That(opts.OrgId).IsNotNull();
        await Assert.That(opts.OrgId!.LongFormat()).IsEqualTo(LegalPersonNumbers.Aktiebolag.Digits);
    }
}
