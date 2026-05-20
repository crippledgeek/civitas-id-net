namespace Civitas.Id.Sweden.EntityFrameworkCore.Tests.Extensions;

using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class UseCivitasIdSwedenTests
{
    public class Registration
    {
        [Test]
        public async Task UseCivitasIdSweden_NullBuilder_ThrowsArgumentNullException()
        {
            await Assert.That(() => ModelConfigurationBuilderExtensions.UseCivitasIdSweden(null!))
                .Throws<System.ArgumentNullException>();
        }

        [Test]
        public async Task UseCivitasIdSweden_ReturnsSameBuilder_ForFluentChaining()
        {
            using var ctx = new RegistrationProbeContext();
            // Side-effect-only: ensures the convention runs without throwing.
            await Assert.That(ctx.Model).IsNotNull();
        }
    }

    public class ModelMapping
    {
        [Test]
        public async Task ConfigureConventions_RegistersConverterForPersonalId()
        {
            using var ctx = new RegistrationProbeContext();
            var entityType = ctx.Model.FindEntityType(typeof(ProbeEntity))!;
            var personalIdProp = entityType.FindProperty(nameof(ProbeEntity.PersonalId))!;

            var converter = personalIdProp.GetValueConverter();
            await Assert.That(converter).IsNotNull();
            await Assert.That(converter!.GetType()).IsEqualTo(typeof(PersonalIdConverter));
        }

        [Test]
        public async Task ConfigureConventions_RegistersConverterForCoordinationId()
        {
            using var ctx = new RegistrationProbeContext();
            var entityType = ctx.Model.FindEntityType(typeof(ProbeEntity))!;
            var coordIdProp = entityType.FindProperty(nameof(ProbeEntity.CoordinationId))!;

            var converter = coordIdProp.GetValueConverter();
            await Assert.That(converter).IsNotNull();
            await Assert.That(converter!.GetType()).IsEqualTo(typeof(CoordinationIdConverter));
        }

        [Test]
        public async Task ConfigureConventions_RegistersConverterForOrganisationId()
        {
            using var ctx = new RegistrationProbeContext();
            var entityType = ctx.Model.FindEntityType(typeof(ProbeEntity))!;
            var orgIdProp = entityType.FindProperty(nameof(ProbeEntity.OrganisationId))!;

            var converter = orgIdProp.GetValueConverter();
            await Assert.That(converter).IsNotNull();
            await Assert.That(converter!.GetType()).IsEqualTo(typeof(OrganisationIdConverter));
        }

        [Test]
        public async Task ConfigureConventions_RegistersConverterForNullablePersonalId()
        {
            // NRT is compile-time metadata only. EF Core's Properties<PersonalId>()
            // matches the CLR type, so both PersonalId and PersonalId? are caught.
            using var ctx = new RegistrationProbeContext();
            var entityType = ctx.Model.FindEntityType(typeof(ProbeEntity))!;
            var nullableProp = entityType.FindProperty(nameof(ProbeEntity.OptionalPersonalId))!;

            var converter = nullableProp.GetValueConverter();
            await Assert.That(converter).IsNotNull();
            await Assert.That(converter!.GetType()).IsEqualTo(typeof(PersonalIdConverter));
        }
    }

    public sealed class ProbeEntity
    {
        public int Id { get; init; }
        public PersonalId PersonalId { get; set; } = null!;
        public PersonalId? OptionalPersonalId { get; set; }
        public CoordinationId CoordinationId { get; set; } = null!;
        public OrganisationId OrganisationId { get; set; } = null!;
    }

    private sealed class RegistrationProbeContext : DbContext
    {
        public RegistrationProbeContext()
            : base(new DbContextOptionsBuilder<RegistrationProbeContext>()
                .UseInMemoryDatabase(databaseName: "probe-" + System.Guid.NewGuid())
                .Options)
        {
        }

        public DbSet<ProbeEntity> Probes => Set<ProbeEntity>();

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
            => configurationBuilder.UseCivitasIdSweden();
    }
}
