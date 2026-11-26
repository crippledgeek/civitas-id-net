# Changelog

All notable changes to `Civitas.Id.Sweden` are documented here.
Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

_When cut, rename this heading to `## [2.0.0] - YYYY-MM-DD` and add a fresh empty
`## [Unreleased]` above it (Keep a Changelog convention)._

### BREAKING CHANGES
- **Civitas.Id.Sweden**: a personnummer, a samordningsnummer or a sole trader's organisation number with a birth
  year before 1800 is no longer accepted, whether the century is written out or inferred. Every entry point, the
  converters, attributes and endpoints included, rejects it as it rejects any other invalid number. This is
  library policy, not law: SKV 704 and the KU schema 12.0 give a person's number the centuries 18, 19 and 20.
  There is no upper bound, and legal persons' numbers are unaffected.
  Earlier versions read and wrote such values, so stored ones no longer read: find and correct them before
  upgrading. One such row fails the whole query with `Civitas.Id.Sweden.EntityFrameworkCore` or
  `Civitas.Id.Sweden.Dapper`, whose READMEs have a query that finds them. (#50)
- **Civitas.Id.Sweden.Fakers**: for a person's birth date before 1800, `Generate`, `GenerateMale` and
  `GenerateFemale` throw `ArgumentOutOfRangeException`, and so does `SwedishOfficialIdFaker.Generate(DateOnly)`,
  whichever type it would draw. `GenerateCentenarian` throws it with a clock before 1900 and, from 1900 to 1909,
  generates no one born before 1800. Other seeded output is unchanged. (#50)
- **Civitas.Id.Sweden.DataAnnotations**: a value of the wrong type, such as an `int`, now fails the four `Valid*Id`
  attributes with the attribute's error message, as it does for any `ValidationAttribute`, where it gave
  "Type Int32 is not a PersonalId." and its like. (#57)

### Fixed
- **Civitas.Id.Sweden**: a number with the year 0000, and a ten-digit number read on a date in one of the first
  centuries, made `TryParse`, `IsValid` and `Parse` throw `ArgumentOutOfRangeException`. They are now rejected
  as any invalid number. (#50)

- **Civitas.Id.Sweden.Json**: corrected the XML doc comments on
  `PersonalIdShortFormatJsonConverter` and `CoordinationIdShortFormatJsonConverter`.
  They wrongly described the `PnrFormat.ShortFormat` wire form as carrying a hyphen or
  `+` centenarian separator, and attributed an internal Stockholm-civil-time read to the
  serialize path. In fact `ShortFormat` emits exactly 10 digits with **no** separator, and
  the Stockholm-civil-time read happens only on **deserialization** (century inference for a
  2-digit-year input). Visible to consumers via IntelliSense; no behavior change. (#9)

- **Civitas.Id.Sweden.AspNetCore**: the OpenAPI example of `CoordinationId` was not a valid number. The
  examples of the person ID types are now numbers from Skatteverket's published test sets. (#23)

- **Civitas.Id.Sweden**: the README example called `ShortFormat` with a format, which does not compile, and
  now calls `Format`. Its valid numbers are from published test data. (#23)

- **Civitas.Id.Sweden.DataAnnotations**: `IsValid(object)` on `ValidPersonalIdAttribute`,
  `ValidCoordinationIdAttribute`, `ValidOrganisationIdAttribute` and `ValidSwedishOfficialIdAttribute` threw
  `ArgumentNullException` for any non-null value, because `ValidationAttribute` calls the context overload with no
  context. It now returns the verdict. Failures with a context are as described under BREAKING CHANGES. (#54)

- **Civitas.Id.Sweden.DataAnnotations**: the four `Valid*Id` attributes ignored `ErrorMessage`,
  `ErrorMessageResourceName` and `ErrorMessageResourceType`, because each overrode `FormatErrorMessage` with a
  fixed text. They now honour them like any `ValidationAttribute`, with `{0}` for the field name. Without one, the
  default text is used. (#57)

- **Civitas.Id.Sweden.AspNetCore**: a malformed value for a typed ID parameter now gives the
  documented 400 in Development too with `UseExceptionHandler()`, where ASP.NET Core answered 500. (#41)

- **Civitas.Id.Sweden.AspNetCore**: the app's and the library's `CustomizeProblemDetails` no longer
  replace each other; both run, the app's last. Apps that registered theirs after the library now
  get the library's `type` on 400 responses, unless they set one. (#43)

- **Civitas.Id.Sweden.AspNetCore**: `CustomizeProblemDetails` also runs on the body written for an
  `InvalidIdNumberException` when no `IProblemDetailsWriter` claims the response, and its context
  now carries the exception. That body is now the same JSON as on the normal path, including
  `instance`, members of derived types, the JSON form of extension values, and null handling; an
  empty `status` is filled in. Errors from the customization and from serializing its result
  surface; the startup check covers `ProblemDetails` itself only. The fallback log events
  (Warning 2, Debug 3) are removed. (#43)

### Changed
- **Civitas.Id.Sweden.AspNetCore**: with `AddCivitasIdSwedenAspNetCore()`, a
  `BadHttpRequestException` that reaches `UseExceptionHandler()` answers with its own status instead
  of 500, on every endpoint and in every environment. An app's own `StatusCodeSelector`,
  `ExceptionHandler` or `ExceptionHandlingPath`, and settings passed to `UseExceptionHandler(...)`,
  are kept. An `IExceptionHandler` service starts from the exception's status instead of 500. (#41)
- **Civitas.Id.Sweden** (internal — no public API or behavior change): `SwedenClock`
  time-zone resolution was refactored to an injectable probe seam to make the fallback
  branches testable. The resolved zone (`Europe/Stockholm`), the probe order, and the
  `InvalidOperationException` message are byte-for-byte unchanged. (#7)

### Security
- **Civitas.Id.Sweden.AspNetCore**: the minimum `Microsoft.AspNetCore.OpenApi` is now 10.0.11,
  which brings Microsoft.OpenApi 2.7.5 and clears GHSA-v5pm-xwqc-g5wc. (#22)

## [1.0.0] - 2026-06-21

Initial v1.0.0 release. Highlights:

- **Civitas.Id.Sweden**: AOT-clean core parsing + validation + formatting for personnummer,
  samordningsnummer, organisationsnummer.
- **Civitas.Id.Sweden.Fakers**: deterministic algorithmic fakers (cryptographically secure
  default; seeded overload for deterministic test fixtures).
- **Civitas.Id.Sweden.Json**: System.Text.Json source-gen converters.
- **Civitas.Id.Sweden.DataAnnotations**: ValidationAttribute family for string DTOs.
- **Civitas.Id.Sweden.AspNetCore**: ASP.NET Core 10 integration — `IExceptionHandler`,
  `ProblemDetails`, OpenAPI schema transformer, dual `Http.Json` + `Mvc.Json` bridge.

See the per-package README on NuGet for usage. See
`docs/superpowers/specs/2026-05-18-aspnetcore-audit-remediation-design.md` for the audit-
remediation design that drove the AspNetCore package's final shape.

### Added
- `Civitas.Id.Sweden.Dapper` companion package: Dapper 2.x `TypeHandler<T>` implementations for `PersonalId`, `CoordinationId`, `OrganisationId` with one-call registration via `CivitasIdSwedenDapperSetup.Register()`. Storage is `varchar(12)` ASCII (legal-person organisations: 10 characters; Enskild firma: 12 characters with lossless century-preserving Encode). Sets `DbType.AnsiString` + `Size = 12` in `SetValue` to enable SQL Server index seeks. SQLite-validated round-trip including the Enskild firma regression guard. Known limitation: Dapper `IN @ids` expansion bypasses TypeHandlers ([Dapper #1649](https://github.com/DapperLib/Dapper/issues/1649)) — README documents the canonical workaround.
- New companion package `Civitas.Id.Sweden.EntityFrameworkCore` — EF Core 10 `ValueConverter`s for `PersonalId`, `CoordinationId`, `OrganisationId`, plus a one-line `UseCivitasIdSweden()` extension on `ModelConfigurationBuilder`. Single-column storage (`varchar(12)` for all three; `OrganisationId` column is variable-width 10/12 chars to support Enskild firma 12-digit canonical roundtrip via `ToPhysicalPersonId()?.LongFormat() ?? LongFormat()`). Polymorphic `SwedishOfficialId` mapping is not in v1.0 — documented as a README pattern using a raw `string` column. Targets `net10.0`. `IsAotCompatible` deliberately omitted while EF Core 10's NativeAOT remains experimental upstream.
- New companion package `Civitas.Id.Sweden.AspNetCore` — full ASP.NET Core 10 integration:
  - `AddCivitasIdSwedenAspNetCore` extension methods (3 overloads: no-arg, `Action<TOptions>`, `IConfiguration`) following the canonical .NET 10 Options pattern (`AddOptions<T>().Configure/Bind().Validate().ValidateOnStart()` + `TryAddEnumerable` for idempotency).
  - `CivitasIdSwedenAspNetCoreOptions` (sealed, mutable properties) + source-generated `[OptionsValidator]` validator; `JsonFormat` propagates into `CivitasIdSwedenJsonOptions.PersonnummerFormat` via DI-aware cross-options bridge.
  - `InvalidIdNumberExceptionHandler` — `IExceptionHandler` translating `InvalidIdNumberException` into RFC 9457 `ProblemDetails` 400 responses with caller-overridable PII redaction (defaults to the exception's built-in `RedactedInput`).
  - `SwedishIdSchemaTransformer` — `IOpenApiSchemaTransformer` augmenting component schemas for `PersonalId`/`CoordinationId`/`OrganisationId`/`SwedishOfficialId` with `format`, `pattern`, `example`, `description`. Registered via `AddOpenApi(opts => opts.AddSchemaTransformer<T>())` on the default document.
  - `CivitasIdHttpJsonOptionsSetup` — `IConfigureOptions<JsonOptions>` that chains `CivitasIdSwedenJsonContext` AND registers the 4 `JsonConverter<T>` instances on `JsonSerializerOptions.Converters` so body deserialization of typed-ID DTOs works under AOT.
  - `WithCivitasIdSwedenMetadata` — `IEndpointConventionBuilder` extension that attaches `ProducesResponseTypeMetadata(400, typeof(ProblemDetails), "application/problem+json")` to individual endpoints or `MapGroup` calls accepting typed Swedish IDs.
  - Uses `FrameworkReference Microsoft.AspNetCore.App` (NOT the legacy `Microsoft.AspNetCore.Http.Abstractions` 2.x NuGet package). AOT-clean (`IL3058` suppressed for the shared-framework `[IsTrimmable]`-but-not-`[IsAotCompatible]` assembly metadata gap).
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

### Fixed
- **Documentation**: `CLAUDE.md` "Critical behavioural rules" — corrected stale claim that `OrganisationId.LongFormat()` returns 12 digits for Enskild firma. It returns 10 digits unconditionally per Lag (1974:174) §4; the 12-digit personnummer view is exposed via `OrganisationId.ToPhysicalPersonId()`.
- **HIGH (domain correctness)**: `OrganisationId.Form` now correctly returns
  `OrganisationForm.AktiebolagOvriga` for orgnummer with the `556…` (pre-2015)
  and `559…` (post-2015) prefixes — previously misclassified as
  `EuropakooperativEgtsEric` due to two-digit form-code extraction. This affected
  essentially every real-world Swedish Aktiebolag (including `5560160680` =
  Telefonaktiebolaget LM Ericsson). Per Bolagsverket's 2015-01-12 announcement,
  the pre-2015 `556…` series was exhausted (last `556999-9997`) and Aktiebolag
  are now issued in the `559…` range (first `559000-0005`). Other `55x` prefixes
  (`5500`–`5559`, excluding `556`) remain `EuropakooperativEgtsEric`.

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

[unreleased]: https://github.com/crippledgeek/civitas-id-net/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/crippledgeek/civitas-id-net/releases/tag/v1.0.0
