using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.EntityFrameworkCore.Tests.Fixtures;
using Civitas.Id.Sweden.Format;
using Microsoft.EntityFrameworkCore;

namespace Civitas.Id.Sweden.EntityFrameworkCore.Tests.Integration;

/// <summary>
///     End-to-end save/reload integration tests against an in-memory SQLite database.
///     Each test gets a fresh <see cref="TestDbContext"/> via <see cref="SqliteFixture"/>.
/// </summary>
public class SqliteRoundtripTests
{
    /// <summary>Tests for the <see cref="Customer"/> entity (PersonalId + nullable CoordinationId).</summary>
    public class CustomerEntity
    {
        /// <summary>
        ///     Saves a <see cref="Customer"/> with a <see cref="PersonalId"/> and reloads it;
        ///     asserts the value survives the SQLite TEXT roundtrip without mutation.
        /// </summary>
        [Test]
        public async Task SaveAndReload_PersonalId_Roundtrips()
        {
            await using var ctx = SqliteFixture.NewInMemoryContext();
            var original = PersonalId.Parse("189001019802");
            ctx.Customers.Add(new Customer { TaxpayerId = original });
            await ctx.SaveChangesAsync();
            ctx.ChangeTracker.Clear();

            var loaded = await ctx.Customers.SingleAsync();

            await Assert.That(loaded.TaxpayerId).IsEqualTo(original);
        }

        /// <summary>
        ///     Saves a <see cref="Customer"/> with a null <see cref="Customer.OptionalCoordinationId"/>
        ///     and reloads it; asserts the property remains null.
        /// </summary>
        [Test]
        public async Task SaveAndReload_NullCoordinationId_StaysNull()
        {
            await using var ctx = SqliteFixture.NewInMemoryContext();
            ctx.Customers.Add(new Customer
            {
                TaxpayerId = PersonalId.Parse("189001019802"),
                OptionalCoordinationId = null
            });
            await ctx.SaveChangesAsync();
            ctx.ChangeTracker.Clear();

            var loaded = await ctx.Customers.SingleAsync();

            await Assert.That(loaded.OptionalCoordinationId).IsNull();
        }

        /// <summary>
        ///     Saves a <see cref="Customer"/> with a non-null <see cref="CoordinationId"/>
        ///     and reloads it; asserts the value survives the roundtrip.
        /// </summary>
        [Test]
        public async Task SaveAndReload_NonNullCoordinationId_Roundtrips()
        {
            await using var ctx = SqliteFixture.NewInMemoryContext();
            var coord = CoordinationId.Parse("191401682396");
            ctx.Customers.Add(new Customer
            {
                TaxpayerId = PersonalId.Parse("189001019802"),
                OptionalCoordinationId = coord
            });
            await ctx.SaveChangesAsync();
            ctx.ChangeTracker.Clear();

            var loaded = await ctx.Customers.SingleAsync();

            await Assert.That(loaded.OptionalCoordinationId).IsEqualTo(coord);
        }
    }

    /// <summary>Tests for the <see cref="Company"/> entity (OrganisationId).</summary>
    public class CompanyEntity
    {
        /// <summary>
        ///     Saves a legal-person <see cref="OrganisationId"/> and reloads it;
        ///     asserts the 10-digit value roundtrips correctly.
        /// </summary>
        [Test]
        public async Task SaveAndReload_LegalPersonOrganisationId_Roundtrips()
        {
            await using var ctx = SqliteFixture.NewInMemoryContext();
            var original = OrganisationId.Parse("5560360793");
            ctx.Companies.Add(new Company { OrgId = original });
            await ctx.SaveChangesAsync();
            ctx.ChangeTracker.Clear();

            var loaded = await ctx.Companies.SingleAsync();

            await Assert.That(loaded.OrgId).IsEqualTo(original);
        }

        /// <summary>
        ///     Integration-level regression guard for Enskild firma.
        ///     Saves a 12-digit Enskild firma <see cref="OrganisationId"/> and reloads it;
        ///     asserts the value roundtrips and the <see cref="OrganisationForm.None"/> form
        ///     is preserved. Fails against any converter that drops the century on store.
        /// </summary>
        [Test]
        public async Task SaveAndReload_EnskildFirmaOrganisationId_Roundtrips()
        {
            await using var ctx = SqliteFixture.NewInMemoryContext();
            var original = OrganisationId.Parse("199001019802");
            ctx.Companies.Add(new Company { OrgId = original });
            await ctx.SaveChangesAsync();
            ctx.ChangeTracker.Clear();

            var loaded = await ctx.Companies.SingleAsync();

            await Assert.That(loaded.OrgId).IsEqualTo(original);
            await Assert.That(loaded.OrgId.Form).IsEqualTo(OrganisationForm.None);
        }
    }

    /// <summary>
    ///     Tests that verify LINQ predicates over Civitas.Id types translate to SQL
    ///     (rather than being evaluated client-side or throwing a translation exception).
    /// </summary>
    public class Queries
    {
        /// <summary>
        ///     Seeds two customers with distinct <see cref="PersonalId"/> values,
        ///     queries with <c>WHERE TaxpayerId = @p</c>, and asserts exactly one hit.
        /// </summary>
        [Test]
        public async Task Where_PersonalIdEquality_TranslatesToSql()
        {
            await using var ctx = SqliteFixture.NewInMemoryContext();
            var target = PersonalId.Parse("189001019802");
            var other = PersonalId.Parse("189001029819");
            ctx.Customers.AddRange(
                new Customer { TaxpayerId = target },
                new Customer { TaxpayerId = other });
            await ctx.SaveChangesAsync();
            ctx.ChangeTracker.Clear();

            var hit = await ctx.Customers.SingleAsync(c => c.TaxpayerId == target);

            await Assert.That(hit.TaxpayerId).IsEqualTo(target);
        }

        /// <summary>
        ///     Seeds three customers, queries with <c>WHERE TaxpayerId IN (@a, @b)</c>,
        ///     and asserts exactly two rows returned.
        /// </summary>
        [Test]
        public async Task Where_PersonalIdInClause_TranslatesToSql()
        {
            await using var ctx = SqliteFixture.NewInMemoryContext();
            var a = PersonalId.Parse("189001019802");
            var b = PersonalId.Parse("189001029819");
            var c = PersonalId.Parse("189001039800");
            ctx.Customers.AddRange(
                new Customer { TaxpayerId = a },
                new Customer { TaxpayerId = b },
                new Customer { TaxpayerId = c });
            await ctx.SaveChangesAsync();
            ctx.ChangeTracker.Clear();

            // Cast to IEnumerable<PersonalId> to pin the LINQ Contains overload
            // and avoid the C# 14 Span<T>.Contains overload-resolution ambiguity
            // (C# 14 CSharp14OverloadResolutionWithSpanBreakingChange).
            IEnumerable<PersonalId> search = [a, b];
            var hits = await ctx.Customers
                .Where(cust => search.Contains(cust.TaxpayerId))
                .ToListAsync();

            await Assert.That(hits.Count).IsEqualTo(2);
        }

        /// <summary>
        ///     Seeds one company and queries with <c>WHERE OrgId = @p</c>; asserts the
        ///     legal-person <see cref="OrganisationId"/> translates correctly.
        /// </summary>
        [Test]
        public async Task Where_OrganisationIdEquality_LegalPerson_TranslatesToSql()
        {
            await using var ctx = SqliteFixture.NewInMemoryContext();
            var target = OrganisationId.Parse("5560360793");
            ctx.Companies.Add(new Company { OrgId = target });
            await ctx.SaveChangesAsync();
            ctx.ChangeTracker.Clear();

            var hit = await ctx.Companies.SingleAsync(c => c.OrgId == target);

            await Assert.That(hit.OrgId).IsEqualTo(target);
        }
    }
}
