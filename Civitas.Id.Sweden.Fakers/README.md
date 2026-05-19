# Civitas.Id.Sweden.Fakers

[![NuGet](https://img.shields.io/nuget/v/Civitas.Id.Sweden.Fakers.svg)](https://www.nuget.org/packages/Civitas.Id.Sweden.Fakers)

Algorithmic fakers producing deterministic, valid Swedish official ID numbers for testing.
Companion to [Civitas.Id.Sweden](https://www.nuget.org/packages/Civitas.Id.Sweden).

The default constructor uses `RandomNumberGenerator` (cryptographically secure). Seeded
and caller-injected `Random` overloads exist for deterministic test fixtures only; do not
rely on them for security-sensitive values.

## Install

```sh
dotnet add package Civitas.Id.Sweden.Fakers
```

## Usage

```csharp
using Civitas.Id.Sweden.Fakers;

// Default — cryptographically random
PersonalId pnr = PersonalIdFaker.Default.Generate();

// Deterministic — same seed → same output
var faker = PersonalIdFaker.WithSeed(42);
PersonalId reproducible = faker.Generate();

// By gender / age
PersonalId male = faker.GenerateMale();
PersonalId female = faker.GenerateFemale();
PersonalId centenarian = faker.GenerateCentenarian();

// Other ID types
CoordinationId samordning = CoordinationIdFaker.Default.Generate();
OrganisationId org = OrganisationIdFaker.Default.GenerateLegalPerson();
OrganisationId enskild = OrganisationIdFaker.Default.GeneratePhysicalPerson();

// Composite faker — mixed bulk generation
var batch = SwedishOfficialIdFaker.Default.Generate(count: 100);
```

## Documentation

See the [main repository](https://github.com/crippledgeek/civitas-id-net) for full documentation.
