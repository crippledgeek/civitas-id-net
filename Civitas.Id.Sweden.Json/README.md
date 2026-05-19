# Civitas.Id.Sweden.Json

[![NuGet](https://img.shields.io/nuget/v/Civitas.Id.Sweden.Json.svg)](https://www.nuget.org/packages/Civitas.Id.Sweden.Json)

`System.Text.Json` source-generated converters for the Swedish official ID types from
[Civitas.Id.Sweden](https://www.nuget.org/packages/Civitas.Id.Sweden). AOT-clean (no
reflection, no `[Serializable]`).

## Install

```sh
dotnet add package Civitas.Id.Sweden.Json
```

## Usage

```csharp
using Microsoft.Extensions.DependencyInjection;
using Civitas.Id.Sweden.Json.Extensions;

var services = new ServiceCollection();
services.AddCivitasIdSwedenJson();   // registers the converters on Http.Json + Mvc.Json
```

Or manually:

```csharp
var options = new JsonSerializerOptions();
options.TypeInfoResolverChain.Insert(0, SwedishIdJsonContext.Default);
```

## Documentation

See the [main repository](https://github.com/crippledgeek/civitas-id-net) for full documentation,
including format selection (`LongFormat` / `ShortFormat` / `ShortFormatWithSeparator`) and
client-side `JsonSerializerContext` configuration for `HttpClient` consumers.
