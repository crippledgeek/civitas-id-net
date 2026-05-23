using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Dapper.Tests.Fixtures;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Civitas.Id.Sweden.Dapper.Tests.Integration;

public class SqliteRoundtripTests
{
    // Register once for the test session — Setup's idempotency makes
    // multiple calls harmless, but [Before(TestSession)] is the idiomatic
    // place to do it.
    [Before(TestSession)]
    public static void RegisterHandlers()
    {
        CivitasIdSwedenDapperSetup.Register();
        // SQLite stores Guids as TEXT; Dapper has no built-in Guid<->string
        // coercion. Register a test-only handler so our Guid PKs roundtrip.
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

    private static SqliteConnection OpenInMemory()
    {
        var c = new SqliteConnection("Data Source=:memory:");
        c.Open();
        return c;
    }

    public class CustomerRoundtrip
    {
        [Test]
        public async Task SaveAndQuery_PersonalId_RoundTrips()
        {
            await using var c = OpenInMemory();
            await c.ExecuteAsync("CREATE TABLE Customer (Id TEXT PRIMARY KEY, TaxpayerId TEXT NOT NULL, OptionalSecondaryId TEXT NULL);");

            var expected = new Customer
            {
                Id = Guid.NewGuid(),
                TaxpayerId = PersonalId.Parse("189001019802")
            };
            await c.ExecuteAsync(
                "INSERT INTO Customer (Id, TaxpayerId, OptionalSecondaryId) VALUES (@Id, @TaxpayerId, @OptionalSecondaryId);",
                expected);

            var loaded = await c.QueryFirstAsync<Customer>("SELECT * FROM Customer WHERE Id = @Id;", new { expected.Id });

            await Assert.That(loaded.TaxpayerId).IsEqualTo(expected.TaxpayerId);
            await Assert.That(loaded.OptionalSecondaryId).IsNull();
        }

        [Test]
        public async Task SaveAndQuery_NullablePersonalId_PreservesNull()
        {
            await using var c = OpenInMemory();
            await c.ExecuteAsync("CREATE TABLE Customer (Id TEXT PRIMARY KEY, TaxpayerId TEXT NOT NULL, OptionalSecondaryId TEXT NULL);");

            var expected = new Customer
            {
                Id = Guid.NewGuid(),
                TaxpayerId = PersonalId.Parse("189001019802"),
                OptionalSecondaryId = null
            };
            await c.ExecuteAsync(
                "INSERT INTO Customer (Id, TaxpayerId, OptionalSecondaryId) VALUES (@Id, @TaxpayerId, @OptionalSecondaryId);",
                expected);

            var loaded = await c.QueryFirstAsync<Customer>("SELECT * FROM Customer WHERE Id = @Id;", new { expected.Id });

            await Assert.That(loaded.OptionalSecondaryId).IsNull();
        }

        [Test]
        public async Task SaveAndQuery_NonNullSecondaryPersonalId_RoundTrips()
        {
            await using var c = OpenInMemory();
            await c.ExecuteAsync("CREATE TABLE Customer (Id TEXT PRIMARY KEY, TaxpayerId TEXT NOT NULL, OptionalSecondaryId TEXT NULL);");

            var expected = new Customer
            {
                Id = Guid.NewGuid(),
                TaxpayerId = PersonalId.Parse("189001019802"),
                OptionalSecondaryId = PersonalId.Parse("189001029819")
            };
            await c.ExecuteAsync(
                "INSERT INTO Customer (Id, TaxpayerId, OptionalSecondaryId) VALUES (@Id, @TaxpayerId, @OptionalSecondaryId);",
                expected);

            var loaded = await c.QueryFirstAsync<Customer>("SELECT * FROM Customer WHERE Id = @Id;", new { expected.Id });

            await Assert.That(loaded.OptionalSecondaryId).IsEqualTo(expected.OptionalSecondaryId);
        }
    }

    public class CoordinationRoundtrip
    {
        [Test]
        public async Task SaveAndQuery_CoordinationId_RoundTrips()
        {
            await using var c = OpenInMemory();
            await c.ExecuteAsync("CREATE TABLE CoordinationHolder (Id TEXT PRIMARY KEY, CoordId TEXT NOT NULL, OptionalCoord TEXT NULL);");

            var expected = new CoordinationHolder
            {
                Id = Guid.NewGuid(),
                CoordId = CoordinationId.Parse("191401682396")
            };
            await c.ExecuteAsync(
                "INSERT INTO CoordinationHolder (Id, CoordId, OptionalCoord) VALUES (@Id, @CoordId, @OptionalCoord);",
                expected);

            var loaded = await c.QueryFirstAsync<CoordinationHolder>("SELECT * FROM CoordinationHolder WHERE Id = @Id;", new { expected.Id });

            await Assert.That(loaded.CoordId).IsEqualTo(expected.CoordId);
        }

        [Test]
        public async Task SaveAndQuery_NullableCoordinationId_PreservesNull()
        {
            await using var c = OpenInMemory();
            await c.ExecuteAsync("CREATE TABLE CoordinationHolder (Id TEXT PRIMARY KEY, CoordId TEXT NOT NULL, OptionalCoord TEXT NULL);");

            var expected = new CoordinationHolder
            {
                Id = Guid.NewGuid(),
                CoordId = CoordinationId.Parse("191401682396"),
                OptionalCoord = null
            };
            await c.ExecuteAsync(
                "INSERT INTO CoordinationHolder (Id, CoordId, OptionalCoord) VALUES (@Id, @CoordId, @OptionalCoord);",
                expected);

            var loaded = await c.QueryFirstAsync<CoordinationHolder>("SELECT * FROM CoordinationHolder WHERE Id = @Id;", new { expected.Id });

            await Assert.That(loaded.OptionalCoord).IsNull();
        }
    }

    public class OrganisationRoundtrip
    {
        [Test]
        public async Task SaveAndQuery_LegalPersonOrganisationId_RoundTrips()
        {
            await using var c = OpenInMemory();
            await c.ExecuteAsync("CREATE TABLE Company (Id TEXT PRIMARY KEY, OrgId TEXT NOT NULL, OptionalSubsidiaryOrgId TEXT NULL);");

            var expected = new Company
            {
                Id = Guid.NewGuid(),
                OrgId = OrganisationId.Parse("5560360793")
            };
            await c.ExecuteAsync(
                "INSERT INTO Company (Id, OrgId, OptionalSubsidiaryOrgId) VALUES (@Id, @OrgId, @OptionalSubsidiaryOrgId);",
                expected);

            var loaded = await c.QueryFirstAsync<Company>("SELECT * FROM Company WHERE Id = @Id;", new { expected.Id });

            await Assert.That(loaded.OrgId).IsEqualTo(expected.OrgId);
        }

        // CRITICAL regression guard: Enskild firma round-trip MUST be lossless.
        // A naive id.LongFormat()-only encode would drop the century to 10 digits
        // and OrganisationId.Parse would fail on re-read. Failing this test means
        // OrganisationIdHandler.Encode was implemented incorrectly. See spec D2.
        [Test]
        public async Task SaveAndQuery_EnskildFirmaOrganisationId_RoundTripsLosslessly()
        {
            await using var c = OpenInMemory();
            await c.ExecuteAsync("CREATE TABLE Company (Id TEXT PRIMARY KEY, OrgId TEXT NOT NULL, OptionalSubsidiaryOrgId TEXT NULL);");

            var expected = new Company
            {
                Id = Guid.NewGuid(),
                OrgId = OrganisationId.Parse("199001019802")
            };
            await c.ExecuteAsync(
                "INSERT INTO Company (Id, OrgId, OptionalSubsidiaryOrgId) VALUES (@Id, @OrgId, @OptionalSubsidiaryOrgId);",
                expected);

            var loaded = await c.QueryFirstAsync<Company>("SELECT * FROM Company WHERE Id = @Id;", new { expected.Id });

            await Assert.That(loaded.OrgId).IsEqualTo(expected.OrgId);
        }

        [Test]
        public async Task SaveAndQuery_NullableOrganisationId_PreservesNull()
        {
            await using var c = OpenInMemory();
            await c.ExecuteAsync("CREATE TABLE Company (Id TEXT PRIMARY KEY, OrgId TEXT NOT NULL, OptionalSubsidiaryOrgId TEXT NULL);");

            var expected = new Company
            {
                Id = Guid.NewGuid(),
                OrgId = OrganisationId.Parse("5560360793"),
                OptionalSubsidiaryOrgId = null
            };
            await c.ExecuteAsync(
                "INSERT INTO Company (Id, OrgId, OptionalSubsidiaryOrgId) VALUES (@Id, @OrgId, @OptionalSubsidiaryOrgId);",
                expected);

            var loaded = await c.QueryFirstAsync<Company>("SELECT * FROM Company WHERE Id = @Id;", new { expected.Id });

            await Assert.That(loaded.OptionalSubsidiaryOrgId).IsNull();
        }

        [Test]
        public async Task SaveAndQuery_EqualityPredicate_LegalPerson_ReturnsRow()
        {
            await using var c = OpenInMemory();
            await c.ExecuteAsync("CREATE TABLE Company (Id TEXT PRIMARY KEY, OrgId TEXT NOT NULL, OptionalSubsidiaryOrgId TEXT NULL);");

            var company = new Company { Id = Guid.NewGuid(), OrgId = OrganisationId.Parse("5560360793") };
            await c.ExecuteAsync(
                "INSERT INTO Company (Id, OrgId, OptionalSubsidiaryOrgId) VALUES (@Id, @OrgId, @OptionalSubsidiaryOrgId);",
                company);

            var found = await c.QueryFirstOrDefaultAsync<Company>(
                "SELECT * FROM Company WHERE OrgId = @OrgId;",
                new { company.OrgId });

            await Assert.That(found).IsNotNull();
            await Assert.That(found!.Id).IsEqualTo(company.Id);
        }
    }
}
