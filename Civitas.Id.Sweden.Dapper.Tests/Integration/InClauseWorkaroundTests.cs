namespace Civitas.Id.Sweden.Dapper.Tests.Integration;

using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Dapper;
using Civitas.Id.Sweden.Dapper.Tests.Fixtures;
using global::Dapper;
using Microsoft.Data.Sqlite;

/// <summary>
///     Regression guard for Dapper issue #1649 — TypeHandlers are NOT invoked
///     for collection elements during <c>WHERE col IN @collection</c> expansion.
///     Documents the canonical workaround: pre-stringify the collection via
///     <c>.Select(id => id.LongFormat()).ToList()</c> before passing to Dapper.
/// </summary>
public class InClauseWorkaroundTests
{
    // Class-scoped setup so this file is independent of SqliteRoundtripTests's
    // [Before(TestSession)] ordering. CivitasIdSwedenDapperSetup.Register() is
    // idempotent (guarded by an Interlocked flag), and SqlMapper.AddTypeHandler
    // tolerates re-registration, so calling here is safe regardless of which
    // test class runs first in the session.
    [Before(Class)]
    public static void RegisterHandlers()
    {
        CivitasIdSwedenDapperSetup.Register();
        SqlMapper.AddTypeHandler(new GuidStringHandler());
    }

    private sealed class GuidStringHandler : SqlMapper.TypeHandler<Guid>
    {
        public override Guid Parse(object value) => Guid.Parse((string)value);
        public override void SetValue(System.Data.IDbDataParameter parameter, Guid value)
        {
            parameter.DbType = System.Data.DbType.String;
            parameter.Value = value.ToString();
        }
    }

    [Test]
    public async Task InClause_WithPreStringifiedCollection_ReturnsExpectedRows()
    {
        using var c = new SqliteConnection("Data Source=:memory:");
        await c.OpenAsync();
        await c.ExecuteAsync("CREATE TABLE Customer (Id TEXT PRIMARY KEY, TaxpayerId TEXT NOT NULL, OptionalSecondaryId TEXT NULL);");

        var customers = new[]
        {
            new Customer { Id = Guid.NewGuid(), TaxpayerId = PersonalId.Parse("189001019802") },
            new Customer { Id = Guid.NewGuid(), TaxpayerId = PersonalId.Parse("189001029819") },
            new Customer { Id = Guid.NewGuid(), TaxpayerId = PersonalId.Parse("189001039800") },
        };
        foreach (var cust in customers)
        {
            await c.ExecuteAsync(
                "INSERT INTO Customer (Id, TaxpayerId, OptionalSecondaryId) VALUES (@Id, @TaxpayerId, @OptionalSecondaryId);",
                cust);
        }

        // Documented workaround: project to IEnumerable<string> of canonical forms.
        var wantedIds = customers.Take(2).Select(cust => cust.TaxpayerId.LongFormat()).ToList();

        var found = (await c.QueryAsync<Customer>(
            "SELECT * FROM Customer WHERE TaxpayerId IN @ids;",
            new { ids = wantedIds })).ToList();

        await Assert.That(found.Count).IsEqualTo(2);
    }

    // Anti-pattern: demonstrates the BROKEN behaviour to ensure consumers know
    // why the workaround exists. Dapper #1649: TypeHandler.SetValue is NOT
    // invoked for each element when an IEnumerable<T> is expanded into an
    // IN-clause. The provider (Microsoft.Data.Sqlite here) then sees raw
    // PersonalId instances as parameter values and throws
    // InvalidOperationException ("No mapping exists from object type
    // Civitas.Id.Sweden.Core.PersonalId to a known managed provider native
    // type.") when binding. If Dapper ever fixes #1649, this throw will stop
    // happening and the assertion below will fail — at which point the
    // workaround in the README can be marked optional/historical.
    [Test]
    public async Task InClause_WithRawIEnumerableOfT_DoesNotInvokeHandler()
    {
        using var c = new SqliteConnection("Data Source=:memory:");
        await c.OpenAsync();
        await c.ExecuteAsync("CREATE TABLE Customer (Id TEXT PRIMARY KEY, TaxpayerId TEXT NOT NULL, OptionalSecondaryId TEXT NULL);");
        await c.ExecuteAsync(
            "INSERT INTO Customer (Id, TaxpayerId, OptionalSecondaryId) VALUES (@Id, @TaxpayerId, NULL);",
            new { Id = Guid.NewGuid().ToString(), TaxpayerId = "189001019802" });

        var wantedIds = new[] { PersonalId.Parse("189001019802") };

        async Task Act() => _ = await c.QueryAsync<Customer>(
            "SELECT * FROM Customer WHERE TaxpayerId IN @ids;",
            new { ids = wantedIds });

        await Assert.That(Act).Throws<InvalidOperationException>();
    }
}
