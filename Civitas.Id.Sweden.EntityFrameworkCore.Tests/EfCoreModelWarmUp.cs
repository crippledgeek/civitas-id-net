using Civitas.Id.Sweden.EntityFrameworkCore.Tests.Fixtures;

namespace Civitas.Id.Sweden.EntityFrameworkCore.Tests;

/// <summary>
///     Warms up EF Core's model cache once, single-threaded, before TUnit dispatches the
///     parallel integration tests in this assembly.
/// </summary>
/// <remarks>
///     EF Core builds the relational model lazily on first use, via a factory annotation that
///     is consumed (removed under a narrow lock) the first time
///     <c>GetRelationalModel</c> runs. When several <see cref="TestDbContext"/> instances trigger
///     that first build concurrently — which TUnit does, running tests in parallel — one thread can
///     remove the factory annotation between another thread's read and use, throwing
///     <c>"The model must be finalized and its runtime dependencies must be initialized before
///     'GetRelationalModel' can be used."</c> The failure is intermittent (it only occurs on the
///     very first concurrent model build) and surfaced once in CI on 2026-06-21 while passing on
///     every other run of identical code.
///
///     Building one context here forces the full model + relational-model build to complete on a
///     single thread and caches it on the context-type-keyed model cache, so every subsequent
///     parallel test hits the finalized fast path and the race window never opens.
///     <see cref="SqliteFixture.NewInMemoryContext"/> calls <c>EnsureCreated()</c>, which requires
///     the relational model and therefore warms exactly the racy path.
///
///     No deterministic unit test accompanies this fix: the defect is a non-deterministic
///     first-use data race that cannot be reproduced reliably without wall-clock thread
///     coordination. Verification is that the 46 EF integration tests continue to pass and the
///     relational model is provably pre-built before any parallel test runs.
/// </remarks>
public static class EfCoreModelWarmUp
{
    [Before(TestSession)]
    public static void WarmUpRelationalModel()
    {
        using var ctx = SqliteFixture.NewInMemoryContext();
        _ = ctx.Model;
    }
}
