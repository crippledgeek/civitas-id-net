# Civitas.Id.Sweden

A standalone .NET 10 library for validating, parsing, and formatting Swedish official ID numbers:

- **Personnummer** — personal identification numbers
- **Samordningsnummer** — coordination numbers (day component offset by +60)
- **Organisationsnummer** — organisation numbers with legal form encoded in positions 1–2

Native AOT compatible. Zero third-party runtime dependencies. Strict analyzers enforced (`TreatWarningsAsErrors=true`, `AnalysisMode=All`).

## Packages

| Package | Purpose |
|---|---|
| `Civitas.Id.Sweden` | Core parser/formatter library |
| `Civitas.Id.Sweden.Fakers` | Algorithmic generators for valid synthetic IDs (test data, demo databases). Default constructor uses cryptographically secure RNG; seeded constructors exist for deterministic test fixtures. |

## Streaming consumers

For high-throughput scenarios — parsing millions of IDs from a CSV file,
log ingestion, ETL pipelines — Civitas.Id.Sweden does not expose async
or streaming APIs of its own. The canonical .NET pattern is to use
`Channel<T>` for back pressure with sync `TryParse(ReadOnlySpan<char>)`
inside the consumer loop:

```csharp
var channel = Channel.CreateBounded<PersonalId>(capacity: 1_000);

// IMPORTANT: anchor "today" to Stockholm civil time for Swedish-civil-age-correct
// century inference — never raw UTC. The library's internal SwedenClock is
// not public; consumers either pass a TimeProvider, take `today` as a function
// argument, or compute Stockholm civil date directly:
var today = DateOnly.FromDateTime(
    TimeZoneInfo.ConvertTime(
        DateTimeOffset.UtcNow,
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm")).DateTime);

// Producer: reads lines, parses, writes to bounded channel (back pressure here).
await foreach (var line in csvReader.ReadAllAsync(ct))
{
    if (PersonalId.TryParse(line.AsSpan(), today, out var id))
        await channel.Writer.WriteAsync(id, ct);
}
channel.Writer.Complete();

// Consumer: pulls parsed IDs at its own pace.
await foreach (var id in channel.Reader.ReadAllAsync(ct))
{
    // downstream work
}
```

This pattern provides:

- **Back pressure**: `Channel.CreateBounded<T>` blocks the producer when the consumer falls behind.
- **Cancellation**: pass `CancellationToken` through both ends.
- **Determinism**: pass an explicit `DateOnly today` to `TryParse` so century inference doesn't drift across the run.
- **AOT cleanliness**: every primitive used (`Channel`, `IAsyncEnumerable`, `ReadOnlySpan<char>`, sync `TryParse`) is AOT-safe.

## Parse overload surface

Each ID type (`PersonalId`, `CoordinationId`, `OrganisationId`) exposes the same parse surface:

| Overload | Clock | Use case |
|---|---|---|
| `Parse(string s)` / `TryParse(string?, out)` | Reads `SwedenClock.Today()` (Stockholm civil time) | Live application code |
| `Parse(ReadOnlySpan<char>, IFormatProvider?)` | (no clock — `IFormatProvider` is unused) | `ISpanParsable<T>` contract |
| `Parse(string, IFormatProvider?)` | (same) | `IParsable<T>` contract |
| `Parse(string, TimeProvider)` / `TryParse(string?, TimeProvider, out)` | Reads `tp.GetUtcNow()` | Dependency-injected clock; testable via `FakeTimeProvider` |
| `Parse(string, DateOnly today)` / `Parse(ReadOnlySpan<char>, DateOnly today)` / `TryParse(*, DateOnly today, out)` | **No clock read** | Deterministic; data migrations, batch processing, deterministic test fixtures |

`SwedishOfficialId.ParseAny` / `TryParseAny` mirror this surface as a composite that dispatches across the three subtypes in resolution order: `PersonalId` → `CoordinationId` → `OrganisationId`.

## License

(See `LICENSE` for details.)
