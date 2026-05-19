# Civitas.Id.Sweden.DataAnnotations

[![NuGet](https://img.shields.io/nuget/v/Civitas.Id.Sweden.DataAnnotations.svg)](https://www.nuget.org/packages/Civitas.Id.Sweden.DataAnnotations)

`DataAnnotations` `ValidationAttribute` family for the Swedish official ID types from
[Civitas.Id.Sweden](https://www.nuget.org/packages/Civitas.Id.Sweden) when bound as `string`
on DTOs.

## Install

```sh
dotnet add package Civitas.Id.Sweden.DataAnnotations
```

## Usage

```csharp
using Civitas.Id.Sweden.DataAnnotations;

public sealed record CustomerDto(
    [property: Required, ValidPersonalId] string PersonalNumber,
    [property: ValidOrganisationId]      string? OrganisationNumber);
```

For records, attributes placed on the **constructor parameter** apply to MVC model binding,
not the property. Use `[property: ValidPersonalId]` to bind the attribute to the generated
property so both MVC and `Validator.TryValidateObject` see it.

Attributes:

| Attribute               | Accepts                                                          |
|-------------------------|------------------------------------------------------------------|
| `ValidPersonalId`       | `PersonalId` (personnummer)                                      |
| `ValidCoordinationId`   | `CoordinationId` (samordningsnummer)                             |
| `ValidOrganisationId`   | `OrganisationId` (organisationsnummer)                           |
| `ValidSwedishOfficialId`| Any of the above (sealed sum-type)                               |

## Documentation

See the [main repository](https://github.com/crippledgeek/civitas-id-net) for full documentation.
