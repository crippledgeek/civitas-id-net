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
    // Four forms of one number from Skatteverket's published test sets. The 10-digit
    // forms infer 1990, the "+" form 1890: both the standard sliding-window century
    // inference and the pre-1900 century inference path.
    [Params(
        "189001019802",        // 12-digit long
        "9001019802",          // 10-digit short
        "900101-9802",         // 10-digit with hyphen
        "900101+9802")]        // 10-digit centenarian (parses as 1890)
    public string Input { get; set; } = null!;

    [Benchmark(Baseline = true)]
    public bool PersonalId_TryParse() => PersonalId.TryParse(Input, out _);

    [Benchmark]
    public bool CoordinationId_TryParse() => CoordinationId.TryParse(Input, out _);

    [Benchmark]
    public bool OrganisationId_TryParse() => OrganisationId.TryParse(Input, out _);
}
