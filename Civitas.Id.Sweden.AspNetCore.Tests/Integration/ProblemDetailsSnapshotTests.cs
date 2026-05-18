namespace Civitas.Id.Sweden.AspNetCore.Tests.Integration;

// Verify.TUnit auto-initializes via [ModuleInitializer]; no [UsesVerify] needed.
// Each test uses its own IntegrationTestFixture instance (per-test pattern, see
// ErrorPathTests.cs). Snapshots pin the ProblemDetails wire format emitted by
// InvalidIdNumberExceptionHandler for each InvalidIdNumberReason category.
public sealed partial class ProblemDetailsSnapshotTests
{
    // traceId is W3C trace-context-formatted and non-deterministic per request:
    // "00-<32 hex>-<16 hex>-<2 hex>". Replace with a stable placeholder.
    [System.Text.RegularExpressions.GeneratedRegex("\"traceId\":\"[^\"]*\"")]
    private static partial System.Text.RegularExpressions.Regex TraceIdRegex();

    private static string ScrubTraceId(string body) =>
        TraceIdRegex().Replace(body, "\"traceId\":\"<scrubbed>\"");


    [Test]
    public async Task PersonalId_Malformed()
    {
        await using var fx = await IntegrationTestFixture.CreateAsync();
        var resp = await fx.Client.GetAsync(new Uri("/customers-strict/19811218bad9", UriKind.Relative));
        var body = await resp.Content.ReadAsStringAsync();
        await Verify(ScrubTraceId(body))
            .UseFileName("PersonalId_Malformed_ProblemDetails");
    }

    [Test]
    public async Task PersonalId_InvalidChecksum()
    {
        await using var fx = await IntegrationTestFixture.CreateAsync();
        var resp = await fx.Client.GetAsync(new Uri("/customers-strict/198112180000", UriKind.Relative));
        var body = await resp.Content.ReadAsStringAsync();
        await Verify(ScrubTraceId(body))
            .UseFileName("PersonalId_InvalidChecksum_ProblemDetails");
    }

    [Test]
    public async Task PersonalId_InvalidDate()
    {
        await using var fx = await IntegrationTestFixture.CreateAsync();
        var resp = await fx.Client.GetAsync(new Uri("/customers-strict/199002309876", UriKind.Relative));
        var body = await resp.Content.ReadAsStringAsync();
        await Verify(ScrubTraceId(body))
            .UseFileName("PersonalId_InvalidDate_ProblemDetails");
    }

    [Test]
    public async Task PersonalId_TooShort()
    {
        await using var fx = await IntegrationTestFixture.CreateAsync();
        var resp = await fx.Client.GetAsync(new Uri("/customers-strict/123", UriKind.Relative));
        var body = await resp.Content.ReadAsStringAsync();
        await Verify(ScrubTraceId(body))
            .UseFileName("PersonalId_TooShort_ProblemDetails");
    }

    [Test]
    public async Task PersonalId_TooLong()
    {
        await using var fx = await IntegrationTestFixture.CreateAsync();
        var resp = await fx.Client.GetAsync(new Uri("/customers-strict/198112189876XXX", UriKind.Relative));
        var body = await resp.Content.ReadAsStringAsync();
        await Verify(ScrubTraceId(body))
            .UseFileName("PersonalId_TooLong_ProblemDetails");
    }

    [Test]
    public async Task OrganisationId_UnknownForm()
    {
        await using var fx = await IntegrationTestFixture.CreateAsync();
        var resp = await fx.Client.GetAsync(new Uri("/orgs-strict/0000001234", UriKind.Relative));
        var body = await resp.Content.ReadAsStringAsync();
        await Verify(ScrubTraceId(body))
            .UseFileName("OrganisationId_UnknownForm_ProblemDetails");
    }
}
