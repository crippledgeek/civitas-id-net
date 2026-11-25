# Civitas.Id.Sweden

[![NuGet](https://img.shields.io/nuget/v/Civitas.Id.Sweden.svg)](https://www.nuget.org/packages/Civitas.Id.Sweden)

AOT-native .NET library for parsing, validating, and formatting Swedish official ID numbers
(personnummer, samordningsnummer, organisationsnummer). Zero third-party runtime dependencies.

## Install

```sh
dotnet add package Civitas.Id.Sweden
```

## Usage

```csharp
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Format;

// Parse and validate
var person = PersonalId.Parse("200001012384");

// Read core facts
DateOnly birth = person.BirthDate;
bool adult = person.IsAdult();

// Format
string long12 = person.LongFormat();              // "200001012384"
string short10 = person.ShortFormat();            // "0001012384"
string short11 = person.Format(PnrFormat.ShortFormatWithSeparator); // "000101-2384"

// TryParse never throws
if (PersonalId.TryParse("198113180000", out var maybe))
{
    // ...
}

// Sealed sum type: PersonalId, CoordinationId, OrganisationId all : SwedishOfficialId
var any = SwedishOfficialId.ParseAny("5560160680");  // OrganisationId
```

## Birth years

A birth year before 1800 is not accepted in a personnummer, a samordningsnummer or a sole trader's organisation
number, whether the century is written out or inferred. This is library policy, not law: SKV 704 and the KU schema
12.0 give a person's number the centuries 18, 19 and 20. There is no upper bound.

## Documentation

See the [main repository](https://github.com/crippledgeek/civitas-id-net) for the full overview,
behavioural rules (Stockholm civil-time anchor, leap-day handling, century inference), and
companion packages (`Civitas.Id.Sweden.Json`, `.DataAnnotations`, `.AspNetCore`, `.Fakers`).
