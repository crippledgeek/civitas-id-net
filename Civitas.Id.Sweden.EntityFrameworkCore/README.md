# Civitas.Id.Sweden.EntityFrameworkCore

EF Core 10 ValueConverters for the [Civitas.Id.Sweden](https://www.nuget.org/packages/Civitas.Id.Sweden) Swedish official-ID types.

## Install

```bash
dotnet add package Civitas.Id.Sweden.EntityFrameworkCore
```

## Wire it once

```csharp
using Civitas.Id.Sweden.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public sealed class AppDbContext : DbContext
{
    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
        => builder.UseCivitasIdSweden();
}
```

That's it. Now your entities can use the domain types directly:

```csharp
public sealed class Customer
{
    public Guid Id { get; init; }
    public PersonalId TaxpayerId { get; set; } = null!;
    public CoordinationId? OptionalCoordinationId { get; set; }
}

public sealed class Company
{
    public Guid Id { get; init; }
    public OrganisationId OrgId { get; set; } = null!;
}
```

## Storage shape

| Type | DDL (SQL Server) | DDL (Postgres) | DDL (SQLite) | Notes |
|---|---|---|---|---|
| `PersonalId` | `char(12) NOT NULL` | `character(12) NOT NULL` | `TEXT NOT NULL` | Always 12 digits. |
| `CoordinationId` | `char(12) NOT NULL` | `character(12) NOT NULL` | `TEXT NOT NULL` | Always 12 digits. |
| `OrganisationId` | `varchar(12) NOT NULL` | `character varying(12) NOT NULL` | `TEXT NOT NULL` | **Variable width: 10 digits for legal-person, 12 for Enskild firma.** Do NOT add `CHECK(LEN = 12)`. |

## LINQ queries

Equality and `IN`-clause queries translate to SQL natively:

```csharp
var id = PersonalId.Parse("189001019802");
var customer = await db.Customers.SingleAsync(c => c.TaxpayerId == id);

var batch = new[] { id1, id2, id3 };
var matches = await db.Customers.Where(c => batch.Contains(c.TaxpayerId)).ToListAsync();
```

Member access on the converted value (`c.TaxpayerId.GetAge(today)`) does NOT translate — EF backlog [#35515](https://github.com/dotnet/efcore/issues/35515). Workaround: denormalise the value to a shadow column at write time.

## Polymorphic `SwedishOfficialId` column

Not supported in v1.0. If your entity has a column that could hold any of the three subtypes (audit log subject, KYC record), store it as a raw `string` and parse on read:

```csharp
public sealed class AuditLog
{
    public Guid Id { get; init; }
    public string SubjectId { get; set; } = null!;  // raw canonical string
}

// Read:
var subject = SwedishOfficialId.ParseAny(log.SubjectId);
```

## `OrganisationForm` queries

`Form` is a runtime property derived from the digits, not a stored column. If you frequently filter by form (`WHERE company.OrgId.Form == OrganisationForm.AktiebolagOvriga`), denormalise:

```csharp
public sealed class Company
{
    public Guid Id { get; init; }
    public OrganisationId OrgId { get; set; } = null!;
    public OrganisationForm Form { get; set; }  // populate in your code
}
```

## MySQL / MariaDB collation note

With `unicode: false`, the Pomelo MySQL provider may emit `CHARACTER SET ascii` or `latin1`. If your server's default collation is `utf8mb4`, joins between an `OrgId` column and a `utf8mb4` column may raise collation-mismatch errors. Override per-property if needed:

```csharp
modelBuilder.Entity<Company>()
    .Property(c => c.OrgId)
    .UseCollation("utf8mb4_bin");
```

## Migration from existing string columns

If your column already stores the library's canonical form (12-digit for `PersonalId`/`CoordinationId`, 10-digit for legal-person `OrganisationId`, 12-digit for Enskild firma) — no migration needed. Add the converter and the column just starts being parsed/formatted automatically.

If your column stores a different format (hyphenated short, "16"-prefixed 12-digit, etc.) — write a one-off `migrationBuilder.Sql` to normalise before adding the converter.

## Corrupted DB row handling

If a stored row contains a malformed Civitas ID string (e.g. from a legacy
data migration, off-path writer, or DB corruption), EF Core's materialiser
will throw `InvalidIdNumberException` (from `Civitas.Id.Sweden.Errors`) when
the property is read.

The exception's `Input` property is REDACTED by design — the raw rejected
string is never carried in the exception chain. However, callers MUST NOT
log `Exception.ToString()` on EF exceptions that may surface this for paths
touching Civitas ID columns, because the call stack may still leak field
names that imply PII presence. Log structured fields (`ex.GetType().Name`,
`ex.Message`) instead.

## AOT

The core `Civitas.Id.Sweden` library is `IsAotCompatible=true`. This companion package is NOT — EF Core 10's NativeAOT support is documented as experimental upstream. Stable AOT is informally targeted for EF Core 12. We will adopt `IsAotCompatible=true` when EF makes it production-ready.

## Advanced wiring — scaffolded DbContexts

For consumers who can't override `ConfigureConventions` (e.g. scaffolded contexts that regeneration would overwrite), register per-property:

```csharp
modelBuilder.Entity<Customer>()
    .Property(c => c.TaxpayerId)
    .HasConversion(new PersonalIdConverter());
```
