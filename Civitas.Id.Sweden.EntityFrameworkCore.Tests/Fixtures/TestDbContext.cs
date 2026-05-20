namespace Civitas.Id.Sweden.EntityFrameworkCore.Tests.Fixtures;

using Civitas.Id.Sweden.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

/// <summary>Minimal <see cref="DbContext"/> used by integration tests.</summary>
public sealed class TestDbContext : DbContext
{
    /// <summary>Initialises a new instance of the <see cref="TestDbContext"/> class.</summary>
    /// <param name="options">The context options (typically wired by <see cref="SqliteFixture"/>).</param>
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }

    /// <summary>Gets the customer entities.</summary>
    public DbSet<Customer> Customers => Set<Customer>();

    /// <summary>Gets the company entities.</summary>
    public DbSet<Company> Companies => Set<Company>();

    /// <inheritdoc/>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        => configurationBuilder.UseCivitasIdSweden();
}

/// <summary>
///     Factory for creating fresh in-memory SQLite contexts for integration tests.
/// </summary>
/// <remarks>
///     SQLite <c>:memory:</c> databases are connection-scoped: the database exists
///     only as long as the connection that created it is open. The connection is NOT
///     owned by the <see cref="DbContext"/>, so it is not disposed when the context
///     is disposed. For short-lived, single-context test units this is acceptable —
///     the connection is released at GC time. If a future test requires the same
///     connection across multiple context instances (e.g. seeding + querying in
///     separate scopes), pass the connection explicitly to both context instances.
/// </remarks>
internal static class SqliteFixture
{
    /// <summary>
    ///     Creates and opens a new in-memory SQLite context with the schema already applied.
    /// </summary>
    /// <returns>A <see cref="TestDbContext"/> backed by a fresh in-memory SQLite database.</returns>
    public static TestDbContext NewInMemoryContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(connection)
            .Options;

        var ctx = new TestDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }
}
