using BenchmarkDotNet.Attributes;
using Civitas.Id.Sweden.Core;
using JetBrains.Annotations;

namespace Civitas.Id.Sweden.Benchmarks;

/// <summary>
///     Parse-throughput benchmark for the three sealed ID records.
///     Establishes a baseline before/after the CRTP dedup refactor.
///     Acceptance: Allocated must remain 0 across all three TryParse paths;
///     ns/op delta within ±20% (halt threshold per spec Section 7.3 given
///     dotnet/runtime#79927).
/// </summary>
[PublicAPI]
[MemoryDiagnoser]
[ShortRunJob]
public class PersonIdParseBenchmarks
{
    // All four values are Luhn-valid forms of the same canonical pin:
    // body 9001010009 → check 9. Two-digit-year inputs route through
    // century inference; the 12-digit form bypasses inference.
    [Params(
        "199001010009",        // 12-digit long
        "9001010009",          // 10-digit short
        "900101-0009",         // 10-digit with hyphen
        "900101+0009")]        // 10-digit centenarian (parses as 1890)
    public string Input { get; set; } = null!;

    [Benchmark(Baseline = true)]
    public bool PersonalId_TryParse() => PersonalId.TryParse(Input, out _);

    [Benchmark]
    public bool CoordinationId_TryParse() => CoordinationId.TryParse(Input, out _);

    [Benchmark]
    public bool OrganisationId_TryParse() => OrganisationId.TryParse(Input, out _);
}
