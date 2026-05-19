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
var person = PersonalId.Parse("198112189876");

// Read core facts
DateOnly birth = person.BirthDate;
bool adult = person.IsAdult();

// Format
string long12 = person.LongFormat();              // "198112189876"
string short10 = person.ShortFormat(PnrFormat.ShortFormat);            // "8112189876"
string short11 = person.ShortFormat(PnrFormat.ShortFormatWithSeparator); // "811218-9876"

// TryParse never throws
if (PersonalId.TryParse("198112180000", out var maybe))
{
    // ...
}

// Sealed sum type: PersonalId, CoordinationId, OrganisationId all : SwedishOfficialId
var any = SwedishOfficialId.ParseAny("5567203067");  // OrganisationId
```

## Documentation

See the [main repository](https://github.com/crippledgeek/civitas-id-net) for the full overview,
behavioural rules (Stockholm civil-time anchor, leap-day handling, century inference), and
companion packages (`Civitas.Id.Sweden.Json`, `.DataAnnotations`, `.AspNetCore`, `.Fakers`).
