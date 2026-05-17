using Civitas.Id.Sweden.Format;
using Civitas.Id.Sweden.Json.Extensions;
using Civitas.Id.Sweden.Json.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Civitas.Id.Sweden.Json.Tests.Extensions;

public class CivitasIdSwedenJsonServiceCollectionExtensionsTests
{
    public class NoArgOverload
    {
        [Test]
        public async Task RegistersDefaults()
        {
            var sp = new ServiceCollection()
                .AddCivitasIdSwedenJson()
                .BuildServiceProvider();
            var opts = sp.GetRequiredService<IOptions<CivitasIdSwedenJsonOptions>>().Value;
            await Assert.That(opts.PersonnummerFormat).IsEqualTo(PnrFormat.LongFormat);
            await Assert.That(opts.OrganisationIdLongFormat).IsTrue();
        }
    }

    public class ActionOverload
    {
        [Test]
        public async Task AppliesConfiguration()
        {
            var sp = new ServiceCollection()
                .AddCivitasIdSwedenJson(o => o.PersonnummerFormat = PnrFormat.ShortFormat)
                .BuildServiceProvider();
            var opts = sp.GetRequiredService<IOptions<CivitasIdSwedenJsonOptions>>().Value;
            await Assert.That(opts.PersonnummerFormat).IsEqualTo(PnrFormat.ShortFormat);
        }

        [Test]
        public async Task NullServices_Throws()
        {
            await Assert.That(() =>
                    ((IServiceCollection)null!).AddCivitasIdSwedenJson(_ => { }))
                .Throws<ArgumentNullException>();
        }

        [Test]
        public async Task NullConfigure_Throws()
        {
            await Assert.That(() =>
                    new ServiceCollection().AddCivitasIdSwedenJson((Action<CivitasIdSwedenJsonOptions>)null!))
                .Throws<ArgumentNullException>();
        }
    }

    public class ConfigurationOverload
    {
        [Test]
        public async Task BindsFromInMemory()
        {
            var cfg = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["PersonnummerFormat"] = "ShortFormat",
                    ["OrganisationIdLongFormat"] = "false"
                })
                .Build();
            var sp = new ServiceCollection()
                .AddCivitasIdSwedenJson(cfg)
                .BuildServiceProvider();
            var opts = sp.GetRequiredService<IOptions<CivitasIdSwedenJsonOptions>>().Value;
            await Assert.That(opts.PersonnummerFormat).IsEqualTo(PnrFormat.ShortFormat);
            await Assert.That(opts.OrganisationIdLongFormat).IsFalse();
        }
    }

    public class Idempotency
    {
        [Test]
        public async Task MultipleCalls_RegisterValidatorOnce()
        {
            var services = new ServiceCollection();
            services.AddCivitasIdSwedenJson();
            services.AddCivitasIdSwedenJson();
            services.AddCivitasIdSwedenJson();

            var validators = services
                .Where(s => s.ServiceType == typeof(IValidateOptions<CivitasIdSwedenJsonOptions>)
                            && s.ImplementationType == typeof(ValidateCivitasIdSwedenJsonOptions))
                .ToList();
            await Assert.That(validators.Count).IsEqualTo(1);
        }
    }
}
