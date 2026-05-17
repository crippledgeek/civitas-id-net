using Civitas.Id.Sweden.AspNetCore.Extensions;
using Civitas.Id.Sweden.AspNetCore.Options;
using Civitas.Id.Sweden.Format;
using Civitas.Id.Sweden.Json.Options;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Options;

namespace Civitas.Id.Sweden.AspNetCore.Tests.Extensions;

public class CivitasIdSwedenAspNetCoreServiceCollectionExtensionsTests
{
    public class NoArgOverload
    {
        [Test]
        public async Task RegistersDefaults()
        {
            var sp = new ServiceCollection()
                .AddCivitasIdSwedenAspNetCore()
                .BuildServiceProvider();
            var opts = sp.GetRequiredService<IOptions<CivitasIdSwedenAspNetCoreOptions>>().Value;
            await Assert.That(opts.JsonFormat).IsEqualTo(PnrFormat.LongFormat);
            await Assert.That(opts.RedactInput).IsNull();
        }

        [Test]
        public async Task RegistersExceptionHandler()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddCivitasIdSwedenAspNetCore();
            var sp = services.BuildServiceProvider();
            var handlers = sp.GetServices<IExceptionHandler>().ToList();
            await Assert.That(handlers).IsNotEmpty();
        }
    }

    public class ActionOverload
    {
        [Test]
        public async Task AppliesConfiguration()
        {
            var sp = new ServiceCollection()
                .AddCivitasIdSwedenAspNetCore(o => o.JsonFormat = PnrFormat.ShortFormat)
                .BuildServiceProvider();
            var opts = sp.GetRequiredService<IOptions<CivitasIdSwedenAspNetCoreOptions>>().Value;
            await Assert.That(opts.JsonFormat).IsEqualTo(PnrFormat.ShortFormat);
        }

        [Test]
        public async Task NullServices_Throws()
        {
            await Assert.That(() =>
                    ((IServiceCollection)null!).AddCivitasIdSwedenAspNetCore(_ => { }))
                .Throws<ArgumentNullException>();
        }

        [Test]
        public async Task NullConfigure_Throws()
        {
            await Assert.That(() =>
                    new ServiceCollection().AddCivitasIdSwedenAspNetCore((Action<CivitasIdSwedenAspNetCoreOptions>)null!))
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
                    ["JsonFormat"] = "ShortFormat"
                })
                .Build();
            var sp = new ServiceCollection()
                .AddCivitasIdSwedenAspNetCore(cfg)
                .BuildServiceProvider();
            var opts = sp.GetRequiredService<IOptions<CivitasIdSwedenAspNetCoreOptions>>().Value;
            await Assert.That(opts.JsonFormat).IsEqualTo(PnrFormat.ShortFormat);
        }

        [Test]
        public async Task NullServices_Throws()
        {
            var cfg = new ConfigurationBuilder().Build();
            await Assert.That(() =>
                    ((IServiceCollection)null!).AddCivitasIdSwedenAspNetCore(cfg))
                .Throws<ArgumentNullException>();
        }

        [Test]
        public async Task NullSection_Throws()
        {
            await Assert.That(() =>
                    new ServiceCollection().AddCivitasIdSwedenAspNetCore((IConfiguration)null!))
                .Throws<ArgumentNullException>();
        }
    }

    public class Idempotency
    {
        [Test]
        public async Task MultipleCalls_RegisterExceptionHandlerOnce()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddCivitasIdSwedenAspNetCore();
            services.AddCivitasIdSwedenAspNetCore();
            services.AddCivitasIdSwedenAspNetCore();

            var sp = services.BuildServiceProvider();
            var handlers = sp.GetServices<IExceptionHandler>()
                .Where(h => h.GetType().Name == "InvalidIdNumberExceptionHandler")
                .ToList();
            await Assert.That(handlers.Count).IsEqualTo(1);
        }

        [Test]
        public async Task MultipleCalls_RegisterValidatorOnce()
        {
            var services = new ServiceCollection();
            services.AddCivitasIdSwedenAspNetCore();
            services.AddCivitasIdSwedenAspNetCore();

            var validators = services
                .Where(s => s.ServiceType == typeof(IValidateOptions<CivitasIdSwedenAspNetCoreOptions>)
                            && s.ImplementationType == typeof(ValidateCivitasIdSwedenAspNetCoreOptions))
                .ToList();
            await Assert.That(validators.Count).IsEqualTo(1);
        }
    }

    public class CrossPackageBridge
    {
        [Test]
        public async Task JsonFormat_PropagatesTo_CivitasIdSwedenJsonOptions()
        {
            var sp = new ServiceCollection()
                .AddCivitasIdSwedenAspNetCore(o => o.JsonFormat = PnrFormat.ShortFormat)
                .BuildServiceProvider();
            var jsonOpts = sp.GetRequiredService<IOptions<CivitasIdSwedenJsonOptions>>().Value;
            await Assert.That(jsonOpts.PersonnummerFormat).IsEqualTo(PnrFormat.ShortFormat);
        }

        [Test]
        public async Task DefaultJsonFormat_PropagatesTo_CivitasIdSwedenJsonOptions()
        {
            var sp = new ServiceCollection()
                .AddCivitasIdSwedenAspNetCore()
                .BuildServiceProvider();
            var jsonOpts = sp.GetRequiredService<IOptions<CivitasIdSwedenJsonOptions>>().Value;
            await Assert.That(jsonOpts.PersonnummerFormat).IsEqualTo(PnrFormat.LongFormat);
        }
    }
}
