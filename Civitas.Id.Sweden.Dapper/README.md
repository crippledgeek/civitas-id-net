# Civitas.Id.Sweden.Dapper

[![NuGet](https://img.shields.io/nuget/v/Civitas.Id.Sweden.Dapper.svg)](https://www.nuget.org/packages/Civitas.Id.Sweden.Dapper/)
[![Build](https://github.com/crippledgeek/civitas-id-net/actions/workflows/ci.yml/badge.svg)](https://github.com/crippledgeek/civitas-id-net/actions/workflows/ci.yml)
[![License](https://img.shields.io/github/license/crippledgeek/civitas-id-net.svg)](https://github.com/crippledgeek/civitas-id-net/blob/master/LICENSE)

Dapper 2.x TypeHandlers for the [Civitas.Id.Sweden](https://www.nuget.org/packages/Civitas.Id.Sweden/) official ID types: `PersonalId` (personnummer), `CoordinationId` (samordningsnummer), `OrganisationId` (organisationsnummer).

## Why

Typed IDs over raw `string`s give you compile-time safety, parse-on-construction validation, and built-in `IsAdult`/`Form` accessors. This companion package lets your Dapper consumers persist them with a single registration call, eliminating per-property `SqlMapper.AddTypeHandler` wiring across projects.

## Quick start

```csharp
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Dapper;
using Dapper;
using Microsoft.Data.Sqlite;

// 1. Register once at application startup (BEFORE any Dapper query):
CivitasIdSwedenDapperSetup.Register();

// 2. Use Civitas.Id types in your POCOs:
public sealed class Customer
{
    public Guid Id { get; init; }
    public PersonalId TaxpayerId { get; init; } = null!;
}

// 3. Query and command normally:
using var c = new SqliteConnection("Data Source=app.db");
c.Open();

c.Execute("INSERT INTO Customer (Id, TaxpayerId) VALUES (@Id, @TaxpayerId);",
    new Customer { Id = Guid.NewGuid(), TaxpayerId = PersonalId.Parse("189001019802") });

var found = c.QueryFirst<Customer>(
    "SELECT * FROM Customer WHERE TaxpayerId = @id;",
    new { id = PersonalId.Parse("189001019802") });
```

## Installation

```bash
dotnet add package Civitas.Id.Sweden.Dapper
```

## Setup

Call `CivitasIdSwedenDapperSetup.Register()` exactly once at application startup, BEFORE any Dapper query executes.

```csharp
CivitasIdSwedenDapperSetup.Register();
```

**Why the ordering matters.** Dapper caches IL deserializers at first-query time per type ([Dapper issue #885](https://github.com/DapperLib/Dapper/issues/885)). Handlers registered AFTER the first query for an affected type will be silently bypassed by the cached deserializer.

**Threading.** `SqlMapper.AddTypeHandler` was made thread-safe in a Dapper 2.x release ([issue #1815](https://github.com/DapperLib/Dapper/issues/1815) closed via PR #2107), but concurrent startup registration remains a code smell. Call `Register()` from a single thread.

**Sync vs async.** Dapper's `Query<T>` / `QueryAsync<T>` / `Execute` / `ExecuteAsync` all share the same `TypeHandlerCache<T>` lookup path; handlers behave identically for sync and async APIs.

## SQLite + `Guid` primary keys — register a `Guid` handler too

SQLite has no native `uniqueidentifier` type, and Dapper 2.x does NOT ship a built-in `Guid` ↔ `TEXT` coercion. If your entities use `Guid` primary keys against SQLite (a common combination — and the exact setup our integration test suite uses), you must register your own `SqlMapper.TypeHandler<Guid>` alongside our handlers, or `INSERT`/`SELECT` round-trips will fail with type mismatch.

```csharp
public sealed class GuidStringHandler : SqlMapper.TypeHandler<Guid>
{
    public override void SetValue(IDbDataParameter parameter, Guid value)
        => parameter.Value = value.ToString();

    public override Guid Parse(object value)
        => Guid.Parse((string)value);
}

// At startup, alongside Civitas registration:
CivitasIdSwedenDapperSetup.Register();
SqlMapper.AddTypeHandler(new GuidStringHandler());
```

This is unrelated to the Civitas types themselves, but the combination (SQLite + `Guid` PKs + Civitas IDs) is common enough that we surface it here. SQL Server, PostgreSQL, and MySQL all have native `Guid`/`uuid` types and do not need this handler.

## Supported types & SQL mappings

| CLR type | Storage | SQL Server / Postgres / MySQL | SQLite | Width |
|---|---|---|---|---|
| `PersonalId` | 12-digit canonical | `varchar(12)` | `TEXT` | fixed 12 |
| `CoordinationId` | 12-digit canonical (day+60) | `varchar(12)` | `TEXT` | fixed 12 |
| `OrganisationId` (legal) | 10-digit canonical | `varchar(12)` ¹ | `TEXT` | 10 |
| `OrganisationId` (Enskild firma) | 12-digit personnummer/samordningsnummer | `varchar(12)` ¹ | `TEXT` | 12 |

¹ The `OrganisationId` column is **variable-width** — legal-person numbers store as 10 characters; Enskild firma as 12 characters. Use `varchar(12)` to fit both. Do NOT use `varchar(10)`; Enskild firma values would be truncated. Do NOT add a `CHECK(LEN(col) = 12)` constraint; legal-person values are 10 characters.

## DDL recommendations

### SQL Server / Azure SQL

```sql
CREATE TABLE Customer (
    Id           uniqueidentifier PRIMARY KEY,
    TaxpayerId   varchar(12) NOT NULL,
    OrgId        varchar(12) NOT NULL    -- variable-width: 10 (legal) or 12 (Enskild firma)
);
```

The handler sets `DbType.AnsiString` + `Size = 12` on every parameter. This produces `@p varchar(12)` parameter declarations that index-seek correctly against `varchar` columns. Without this, Dapper's default `nvarchar(MAX)` parameter type causes `CONVERT_IMPLICIT` that defeats the seek (143× more logical reads measured in published benchmarks).

### PostgreSQL

```sql
CREATE TABLE customer (
    id           uuid PRIMARY KEY,
    taxpayer_id  character varying(12) NOT NULL,
    org_id       character varying(12) NOT NULL
);
```

### SQLite

```sql
CREATE TABLE Customer (
    Id           TEXT PRIMARY KEY,
    TaxpayerId   TEXT NOT NULL,
    OrgId        TEXT NOT NULL
);
```

### MySQL / MariaDB

```sql
CREATE TABLE Customer (
    Id          CHAR(36) PRIMARY KEY,
    TaxpayerId  varchar(12) CHARACTER SET ascii NOT NULL,
    OrgId       varchar(12) CHARACTER SET ascii NOT NULL
);
```

`CHARACTER SET ascii` matches the `DbType.AnsiString` parameter we send. Without it, joins between ASCII parameter and `utf8mb4` column can raise collation-mismatch errors.

## Known limitations

### `WHERE col IN @ids` with `IEnumerable<PersonalId>` — handler bypassed

Dapper expands `IN @ids` to `IN (@ids0, @ids1, …)` but DOES NOT call registered `TypeHandler` instances for the expanded elements ([issue #1649](https://github.com/DapperLib/Dapper/issues/1649), open). Each element falls back to `Convert.ToString` / `obj.ToString()`. For our records, `ToString()` returns the record's default representation, not the 12-digit canonical, so the query returns no rows.

**Workaround** — project to `IEnumerable<string>` before passing:

```csharp
// Wrong — handler bypassed, ToString fallback used:
var ids = new[] { PersonalId.Parse("189001019802"), PersonalId.Parse("189001029819") };
var rows = await connection.QueryAsync<Customer>(
    "SELECT * FROM Customer WHERE TaxpayerId IN @ids;", new { ids });   // returns 0 rows

// Right — pre-convert to canonical strings:
var ids = personalIdList.Select(id => id.LongFormat()).ToList();
var rows = await connection.QueryAsync<Customer>(
    "SELECT * FROM Customer WHERE TaxpayerId IN @ids;", new { ids });   // works
```

### Polymorphic `SwedishOfficialId` column — not supported

There is no `SwedishOfficialIdHandler` for the abstract base. If your column may hold any of personnummer / samordningsnummer / organisationsnummer (e.g. an audit-log subject column), use a raw `string` column and call `SwedishOfficialId.ParseAny(s)` on read.

### Queryable `OrganisationForm` — not supported via handler

`OrganisationId.Form` is computed from the number's prefix and is not stored as a separate column. If you need `WHERE company.OrgId.Form == OrganisationForm.AktiebolagOvriga` queries, add a denormalised `Form` shadow column on your entity and populate it from `OrgId.Form` in your save path.

### NativeAOT — not supported

Vanilla Dapper 2.x uses runtime IL generation and reflection that are incompatible with NativeAOT. For AOT scenarios, use [Dapper.AOT](https://www.nuget.org/packages/Dapper.AOT/) (a separate source-generator package). A `Civitas.Id.Sweden.Dapper.Aot` companion is tracked for a future v1.1 release, gated on Dapper.AOT confirming its `TypeHandlerAttribute` source-gen integration.

## Framework support

- **.NET 10** (only target framework; matches the rest of the Civitas.Id.Sweden family).
- **Dapper 2.1.x** (floating patch).

## Feedback

Open issues at [github.com/crippledgeek/civitas-id-net](https://github.com/crippledgeek/civitas-id-net).

## License

MIT. See [LICENSE](https://github.com/crippledgeek/civitas-id-net/blob/master/LICENSE).

## Build & Test

```bash
# From repo root:
dotnet build Civitas.Id.Sweden.Dapper/Civitas.Id.Sweden.Dapper.csproj
dotnet test  Civitas.Id.Sweden.Dapper.Tests/Civitas.Id.Sweden.Dapper.Tests.csproj
```

Test coverage target: ≥ 90% line coverage on the companion package.
