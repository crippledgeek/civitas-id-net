# Changelog

All notable changes to `Civitas.Id.Sweden` are documented here.
Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added
- New companion package `Civitas.Id.Sweden.DataAnnotations` — System.ComponentModel.DataAnnotations validation attributes:
  - `ValidSwedishOfficialIdAttribute` (composite — accepts any of the three subtypes via `SwedishOfficialId.TryParseAny`).
  - `ValidPersonalIdAttribute`, `ValidCoordinationIdAttribute`, `ValidOrganisationIdAttribute` (type-strict).
  - Each attribute accepts both `string` and typed-instance values; `null` short-circuits to `ValidationResult.Success` per DataAnnotations convention (use `[Required]` for null-rejection separately).
  - Cross-type rejection enforced at instance level for all three per-type attributes. Note: at the string level, a 12-digit personnummer can legitimately parse as `OrganisationId` (Enskild firma per Lag 1974:174) — documented inline.
  - Zero dependency on ASP.NET Core or System.Text.Json. Consumable standalone by MAUI, WPF, Avalonia, Blazor.
- New companion package `Civitas.Id.Sweden.Json` — System.Text.Json integration:
  - Per-type `JsonConverter<T>` for `PersonalId`, `CoordinationId`, `OrganisationId`.
  - Polymorphic `SwedishOfficialIdJsonConverter` materializing the correct subtype via `TryParseAny`.
  - Source-generated `CivitasIdSwedenJsonContext : JsonSerializerContext` (public, composable via `TypeInfoResolverChain.Add`) for AOT-friendly serialization.
  - `CivitasIdSwedenJsonOptions` (sealed class, mutable properties) + source-generated `[OptionsValidator]` validator.
  - `AddCivitasIdSwedenJson` extension methods (3 overloads: no-arg, `Action<TOptions>`, `IConfiguration`) following the canonical .NET 10 Options pattern (`AddOptions<T>().Configure().Validate().ValidateOnStart()` + `TryAddEnumerable` for idempotency).
  - All converters AOT-clean, all carry `[PublicAPI]`. Zero dependency on ASP.NET Core.
- `PersonalIdTypeConverter`, `CoordinationIdTypeConverter`, `OrganisationIdTypeConverter`,
  `SwedishOfficialIdTypeConverter` — `System.ComponentModel.TypeConverter` implementations
  in `Civitas.Id.Sweden.TypeConverters` namespace. Closes the IConfiguration / Options
  reflection-binder gap so consumers can bind `PersonalId`-typed settings from
  `appsettings.json` without writing custom converters. AOT-clean (no `Reflection.Emit`).
- `[TypeConverter]` attributes applied to `PersonalId`, `CoordinationId`, `OrganisationId`
  record declarations, enabling automatic discovery via `TypeDescriptor.GetConverter`.
  Round-trip through `LongFormat()` (10/12-digit canonical form per type).
- New companion package `Civitas.Id.Sweden.Fakers` providing algorithmic generators for valid Swedish official IDs (`PersonalIdFaker`, `CoordinationIdFaker`, `OrganisationIdFaker`, `SwedishOfficialIdFaker`). Pattern B distribution: regular NuGet, consumer-choice (test or production). AOT-clean. Closes deferred task #4. **Default constructor uses cryptographically secure RNG** (`System.Security.Cryptography.RandomNumberGenerator.GetInt32`). The seeded `(int seed)` and caller-injected `(Random)` constructor overloads exist for deterministic test fixtures only — do NOT rely on those paths for security-sensitive scenarios.
- `OrganisationId.FromValidated(string)` internal factory — used by the fakers package to skip redundant Luhn re-validation. Not exposed publicly.
- JetBrains code-annotation attributes applied to the public surface via `JetBrains.Annotations.Sources` (compile-time only, `PrivateAssets="all"`, zero IL impact, no transitive consumer dependency): `[PublicAPI]` on every public type, `[Pure]` on stateless query methods (`Parse`/`TryParse`/`IsValid`/`Format`/`LongFormat`/`ShortFormat`/`GetAge(DateOnly|TimeProvider)`/etc.), `[ContractAnnotation]` stacked alongside `[NotNullWhen]` on every `TryParse*` overload, `[NonNegativeValue]` on `GetAge`. Improves consumer-side static analysis (Rider/ReSharper) without affecting Roslyn behavior.
- `GetAge(TimeProvider)`, `IsAdult(TimeProvider)`, `IsChild(TimeProvider)` overloads on `PersonalId` and `CoordinationId`. The provider's `GetLocalNow()` determines "today" — caller-controlled.
- `Parse(string, TimeProvider)` and `TryParse(string, TimeProvider, out)` overloads on `PersonalId` and `CoordinationId`. The provider's local year drives century inference.
- `Format(PnrFormat, TimeProvider)` overload on `PersonalId` and `CoordinationId`. The provider's local date determines the centenarian "+" separator.
- `ParseAny(string, TimeProvider)` and `TryParseAny(string, TimeProvider, out)` on `SwedishOfficialId`.
- Test-only dependency on `Microsoft.Extensions.TimeProvider.Testing` (`FakeTimeProvider`) for deterministic time-injection tests.
- `Parse(string, DateOnly today)`, `Parse(ReadOnlySpan<char>, DateOnly today)`, `TryParse(string?, DateOnly today, out)`, `TryParse(ReadOnlySpan<char>, DateOnly today, out)` overloads on `PersonalId`, `CoordinationId`, `OrganisationId`. Fully deterministic — does not read any clock. The `today` parameter is the reference date used for two-digit-year century inference (PersonalId, CoordinationId; ignored on OrganisationId for API-consistency only). Closes deferred task #10.
- `Format(PnrFormat, DateOnly today)` overload on `PersonalId`, `CoordinationId`, and `OrganisationId`. The `today` parameter determines the centenarian `+`/`-` separator deterministically (active for PersonalId, CoordinationId, and Enskild firma OrganisationId; legal-person OrganisationId always uses `-`).
- `ParseAny(string, DateOnly today)`, `ParseAny(ReadOnlySpan<char>, DateOnly today)`, `TryParseAny(string?, DateOnly today, out)`, `TryParseAny(ReadOnlySpan<char>, DateOnly today, out)` on `SwedishOfficialId`. Composite dispatch delegates to each subtype's DateOnly TryParse.
- All new DateOnly overloads carry `[Pure]` (deterministic, no clock read); the `TryParse*` overloads carry `[ContractAnnotation]` for Rider/ReSharper null-flow analysis alongside `[MaybeNullWhen(false)]` on the `out` parameter for Roslyn.

### Removed (corrective API break in `Civitas.Id.Sweden.Fakers`)
- `IOrganisationIdFaker.GeneratePhysicalPerson(TimeProvider? timeProvider = null)` — the `TimeProvider?` parameter was removed. The interface XML doc claimed the provider influenced the underlying birth date's century, but no implementation honoured it: the bearer is generated by `PersonalIdFaker.Generate()` / `CoordinationIdFaker.Generate()` using a 1970–2019 birth-date range, where century is implicit. The parameter was non-functional. Callers passing an explicit `TimeProvider` should drop the argument; the resulting orgnummer is unchanged in semantics.

### Changed (legal-correctness fix — silent behavior change)
- The zero-argument `GetAge()`, `IsAdult()`, `IsChild()` methods on `PersonalId` and `CoordinationId` now anchor "today" to **Sweden's civil timezone** (`Europe/Stockholm`) instead of UTC. Previously, in UTC-deployed containers (the AWS/GCP/Azure default) these methods returned the wrong age for the 1–2 hour window between Stockholm midnight and UTC midnight on a birthday. The new behavior matches Föräldrabalken 9 kap 1 § + Lag (1930:173) §1 + SFS 1979:988 / SFS 2001:127.
- The same Stockholm pin applies to:
  - `Parse(string)` century inference (year-end UTC vs. Stockholm boundary).
  - `Format(PnrFormat)` centenarian "+" separator inference.
- For deterministic results in tests, callers should pass an explicit `DateOnly today` or `TimeProvider`.

### Migration notes
- No public API was removed. Existing zero-arg method signatures are unchanged.
- The behavior change is only observable for callers running in non-Stockholm host timezones (typically UTC containers) AT the boundary instant of a birthday or year-rollover. Passing `DateOnly.FromDateTime(DateTime.UtcNow)` as the explicit `DateOnly today` argument reproduces the prior behavior.
- The legal-compliance basis for the Stockholm civil-time anchor is documented in the README and in the XML doc-remarks on the affected methods. Primary statutory sources: Föräldrabalken (1949:381) 9 kap 1 §, Lag (1930:173) §1, Förordning (1979:988) om svensk normaltid, Förordning (2001:127) om sommartid.
