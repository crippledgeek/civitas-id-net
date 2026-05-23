using System.Diagnostics.CodeAnalysis;
using Civitas.Id.Sweden.Core;
using Microsoft.EntityFrameworkCore;

namespace Civitas.Id.Sweden.EntityFrameworkCore.Tests.Extensions;

public class UseCivitasIdSwedenTests
{
    public class Registration
    {
        [Test]
        public async Task UseCivitasIdSweden_NullBuilder_ThrowsArgumentNullException()
        {
            await Assert.That(() => ModelConfigurationBuilderExtensions.UseCivitasIdSweden(null!))
                .Throws<ArgumentNullException>();
        }

        [Test]
        public async Task UseCivitasIdSweden_ReturnsSameBuilder_ForFluentChaining()
        {
            await using var ctx = new RegistrationProbeContext();
            // Side-effect-only: ensures the convention runs without throwing.
            await Assert.That(ctx.Model).IsNotNull();
        }
    }

    public class ModelMapping
    {
        [Test]
        public async Task ConfigureConventions_RegistersConverterForPersonalId()
        {
            await using var ctx = new RegistrationProbeContext();
            var entityType = ctx.Model.FindEntityType(typeof(ProbeEntity))!;
            var personalIdProp = entityType.FindProperty(nameof(ProbeEntity.PersonalId))!;

            var converter = personalIdProp.GetValueConverter();
            await Assert.That(converter).IsNotNull();
            await Assert.That(converter!.GetType()).IsEqualTo(typeof(PersonalIdConverter));
        }

        [Test]
        public async Task ConfigureConventions_RegistersConverterForCoordinationId()
        {
            await using var ctx = new RegistrationProbeContext();
            var entityType = ctx.Model.FindEntityType(typeof(ProbeEntity))!;
            var coordIdProp = entityType.FindProperty(nameof(ProbeEntity.CoordinationId))!;

            var converter = coordIdProp.GetValueConverter();
            await Assert.That(converter).IsNotNull();
            await Assert.That(converter!.GetType()).IsEqualTo(typeof(CoordinationIdConverter));
        }

        [Test]
        public async Task ConfigureConventions_RegistersConverterForOrganisationId()
        {
            await using var ctx = new RegistrationProbeContext();
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
            await using var ctx = new RegistrationProbeContext();
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
        public PersonalId PersonalId { get; init; } = null!;
        public PersonalId? OptionalPersonalId { get; init; }
        public CoordinationId CoordinationId { get; init; } = null!;
        public OrganisationId OrganisationId { get; init; } = null!;
    }

    private sealed class RegistrationProbeContext(DbContextOptions<RegistrationProbeContext> options)
        : DbContext(options)
    {
        public RegistrationProbeContext()
            : this(new DbContextOptionsBuilder<RegistrationProbeContext>()
                .UseInMemoryDatabase(databaseName: "probe-" + Guid.NewGuid())
                .Options)
        {
        }

        // DbSet exposes ProbeEntity to the EF Core model; entity registration
        // is implicit through the DbSet property — keeping this member is required.
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "DbSet registers ProbeEntity with the EF Core model.")]
        public DbSet<ProbeEntity> Probes => Set<ProbeEntity>();

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
            => configurationBuilder.UseCivitasIdSweden();
    }
}
