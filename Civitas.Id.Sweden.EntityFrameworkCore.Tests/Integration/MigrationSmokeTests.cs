namespace Civitas.Id.Sweden.EntityFrameworkCore.Tests.Integration;

using Civitas.Id.Sweden.EntityFrameworkCore.Tests.Fixtures;

/// <summary>
///     Smoke tests that verify EF Core property facets (MaxLength, Unicode, Nullable)
///     match the documented column shape after <c>EnsureCreated()</c>.
///     These tests exercise the model-snapshot path — DDL driven by
///     <see cref="Microsoft.EntityFrameworkCore.Infrastructure.ModelSnapshot"/> metadata.
/// </summary>
public class MigrationSmokeTests
{
    /// <summary>
    ///     Asserts that all three Civitas.Id property types carry <c>MaxLength(12)</c>
    ///     and <c>IsUnicode(false)</c> facets, as declared by each converter's
    ///     <see cref="Microsoft.EntityFrameworkCore.Storage.ValueConversion.ConverterMappingHints"/>.
    /// </summary>
    public class PropertyFacets
    {
        /// <summary>
        ///     Verifies that <see cref="Customer.TaxpayerId"/> (<see cref="Civitas.Id.Sweden.Core.PersonalId"/>)
        ///     has <c>MaxLength(12)</c> and <c>IsUnicode(false)</c>.
        /// </summary>
        [Test]
        public async Task PersonalId_HasMaxLengthTwelveAscii()
        {
            await using var ctx = SqliteFixture.NewInMemoryContext();
            var entityType = ctx.Model.FindEntityType(typeof(Customer))!;
            var prop = entityType.FindProperty(nameof(Customer.TaxpayerId))!;

            await Assert.That(prop.GetMaxLength()).IsEqualTo(12);
            await Assert.That(prop.IsUnicode()).IsFalse();
        }

        /// <summary>
        ///     Verifies that <see cref="Customer.OptionalCoordinationId"/> (<see cref="Civitas.Id.Sweden.Core.CoordinationId"/>?)
        ///     has <c>MaxLength(12)</c> and <c>IsUnicode(false)</c>.
        /// </summary>
        [Test]
        public async Task CoordinationId_HasMaxLengthTwelveAscii()
        {
            await using var ctx = SqliteFixture.NewInMemoryContext();
            var entityType = ctx.Model.FindEntityType(typeof(Customer))!;
            var prop = entityType.FindProperty(nameof(Customer.OptionalCoordinationId))!;

            await Assert.That(prop.GetMaxLength()).IsEqualTo(12);
            await Assert.That(prop.IsUnicode()).IsFalse();
        }

        /// <summary>
        ///     Verifies that <see cref="Company.OrgId"/> (<see cref="Civitas.Id.Sweden.Core.OrganisationId"/>)
        ///     has <c>MaxLength(12)</c> and <c>IsUnicode(false)</c>.
        /// </summary>
        [Test]
        public async Task OrganisationId_HasMaxLengthTwelveAscii()
        {
            await using var ctx = SqliteFixture.NewInMemoryContext();
            var entityType = ctx.Model.FindEntityType(typeof(Company))!;
            var prop = entityType.FindProperty(nameof(Company.OrgId))!;

            await Assert.That(prop.GetMaxLength()).IsEqualTo(12);
            await Assert.That(prop.IsUnicode()).IsFalse();
        }
    }

    /// <summary>
    ///     Asserts that nullable properties registered via the Civitas.Id conventions
    ///     are reflected correctly in the EF Core model.
    /// </summary>
    public class NullableMatching
    {
        /// <summary>
        ///     Verifies that <see cref="Customer.OptionalCoordinationId"/> is both nullable
        ///     in the EF Core model and carries a value converter (i.e. the convention wired
        ///     the converter to the nullable CLR type).
        /// </summary>
        [Test]
        public async Task OptionalCoordinationId_IsRegisteredAndNullable()
        {
            await using var ctx = SqliteFixture.NewInMemoryContext();
            var entityType = ctx.Model.FindEntityType(typeof(Customer))!;
            var prop = entityType.FindProperty(nameof(Customer.OptionalCoordinationId))!;

            await Assert.That(prop.IsNullable).IsTrue();
            await Assert.That(prop.GetValueConverter()).IsNotNull();
        }
    }
}
