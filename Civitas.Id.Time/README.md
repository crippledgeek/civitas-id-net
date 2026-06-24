# Civitas.Id.Time

[![NuGet](https://img.shields.io/nuget/v/Civitas.Id.Time.svg)](https://www.nuget.org/packages/Civitas.Id.Time)

ID-agnostic civil-time anchor for the Civitas.Id family. `CivilClock` provides the current
calendar date in a jurisdiction's legally-authoritative civil timezone, resolved via an
IANA-first / Windows-CLDR-fallback probe. NativeAOT-compatible; zero third-party runtime
dependencies.

## Install

```sh
dotnet add package Civitas.Id.Time
```

## Usage

```csharp
using Civitas.Id.Time;

// Pre-defined jurisdiction:
DateOnly today = Sweden.Stockholm.Today();          // civil date in Europe/Stockholm
TimeZoneInfo zone = Sweden.Stockholm.TimeZone;

// Deterministic in tests — inject the UTC instant via TimeProvider
// (the jurisdiction's zone is fixed; TimeProvider.LocalTimeZone is ignored):
DateOnly fixedDate = Sweden.Stockholm.Today(TimeProvider.System);
```

Define a clock for another jurisdiction from its IANA id plus a Windows-CLDR fallback:

```csharp
var paris = new CivilClock(new TimeZoneIds("Europe/Paris", "Romance Standard Time"));
DateOnly todayInParis = paris.Today();
```

## Resolution & deployment

The zone is resolved lazily: the IANA id is probed first, then the Windows fallback. If neither
resolves, an `InvalidOperationException` is thrown with deployment guidance — there is no silent
fixed-offset fallback, so a misconfigured host fails loudly instead of returning wrong dates.

On minimal containers without timezone data (e.g. Alpine), install it: `apk add --no-cache tzdata`.
`InvariantGlobalization=true` is not supported.

## Documentation

See the [main repository](https://github.com/crippledgeek/civitas-id-net) for full documentation.
This package underpins the Stockholm-anchored age calculations in
[Civitas.Id.Sweden](https://www.nuget.org/packages/Civitas.Id.Sweden).
