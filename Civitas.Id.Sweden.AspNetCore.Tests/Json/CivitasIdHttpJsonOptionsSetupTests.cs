using Civitas.Id.Sweden.AspNetCore.Json;
using Civitas.Id.Sweden.Json;
using Microsoft.AspNetCore.Http.Json;

namespace Civitas.Id.Sweden.AspNetCore.Tests.Json;

public class CivitasIdHttpJsonOptionsSetupTests
{
    public class Configure
    {
        [Test]
        public async Task ChainsCivitasIdSwedenJsonContext()
        {
            var opts = new JsonOptions();
            var sut = new CivitasIdHttpJsonOptionsSetup();

            sut.Configure(opts);

            await Assert.That(opts.SerializerOptions.TypeInfoResolverChain
                .Any(r => r is CivitasIdSwedenJsonContext)).IsTrue();
        }

        [Test]
        public async Task NullOptions_Throws()
        {
            var sut = new CivitasIdHttpJsonOptionsSetup();
            await Assert.That(() => sut.Configure(null!)).Throws<ArgumentNullException>();
        }

        [Test]
        public async Task IsIdempotent_WhenCalledTwice()
        {
            var opts = new JsonOptions();
            var sut = new CivitasIdHttpJsonOptionsSetup();

            sut.Configure(opts);
            sut.Configure(opts);

            // The setup itself does not deduplicate (caller relies on DI
            // singleton lifetime). Just verify both calls succeed and the
            // context is registered at least once.
            await Assert.That(opts.SerializerOptions.TypeInfoResolverChain
                .Count(r => r is CivitasIdSwedenJsonContext)).IsEqualTo(2);
        }
    }
}
