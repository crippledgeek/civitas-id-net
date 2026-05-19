using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using Civitas.Id.Sweden.AspNetCore.Endpoints;
using Civitas.Id.Sweden.AspNetCore.Extensions;
using Civitas.Id.Sweden.AspNetCore.OpenApi;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.DataAnnotations;
using Civitas.Id.Sweden.Json;
using Civitas.Id.Sweden.Json.Converters;

Console.WriteLine("=== Civitas.Id.Sweden AOT smoke test ===");

// ── 1. Core: parse + format ─────────────────────────────────────────────────
var pid = PersonalId.Parse("189001019802");
Assert.Equal(pid.LongFormat(), "189001019802", "PersonalId.LongFormat");

var cid = CoordinationId.Parse("191401682396");
Assert.Equal(cid.LongFormat(), "191401682396", "CoordinationId.LongFormat");

var oid = OrganisationId.Parse("5560160680");
Assert.Equal(oid.LongFormat(), "5560160680", "OrganisationId.LongFormat (legal-person 10-digit)");

var any = SwedishOfficialId.ParseAny("189001019802");
Assert.Equal(any.GetType().Name, "PersonalId", "SwedishOfficialId.ParseAny dispatch");
Console.WriteLine("  ✓ Core parse + format + polymorphic dispatch");

// ── 2. TypeConverter discovery ──────────────────────────────────────────────
var parsedViaTypeConverter = TypeConverterProbe.RoundTrip("189001019802");
Assert.Equal(parsedViaTypeConverter.LongFormat(), "189001019802", "TypeConverter round-trip");
Console.WriteLine("  ✓ TypeConverter discovery + round-trip");

// ── 3. STJ source-gen ───────────────────────────────────────────────────────
var stjOptions = new JsonSerializerOptions
{
    Converters = { new PersonalIdLongFormatJsonConverter() },
    TypeInfoResolver = CivitasIdSwedenJsonContext.Default
};
var pidTypeInfo = (System.Text.Json.Serialization.Metadata.JsonTypeInfo<PersonalId>)stjOptions.GetTypeInfo(typeof(PersonalId));

var stjJson = JsonSerializer.Serialize(pid, pidTypeInfo);
Assert.Equal(stjJson, "\"189001019802\"", "STJ source-gen Serialize");

var stjRound = JsonSerializer.Deserialize(stjJson, pidTypeInfo)!;
Assert.Equal(stjRound.LongFormat(), "189001019802", "STJ source-gen Deserialize round-trip");
Console.WriteLine("  ✓ STJ source-gen Serialize + Deserialize");

// ── 4. DataAnnotations attribute ────────────────────────────────────────────
DataAnnotationsProbe.Run();
Console.WriteLine("  ✓ DataAnnotations ValidPersonalIdAttribute");

// ── 5. Minimal API host (AspNetCore) ────────────────────────────────────────
var builder = WebApplication.CreateSlimBuilder(args);
builder.Services.AddCivitasIdSwedenAspNetCore();
// OpenAPI is opt-in (Phase 2): register and attach Civitas schema metadata.
builder.Services.AddOpenApi(static opts => opts.AddCivitasIdSwedenSchemas());

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.MapOpenApi();

var typed = app.MapGroup("").WithCivitasIdSwedenMetadata();
typed.MapGet("/customers/{id}", (PersonalId id) => Results.Text(id.LongFormat()));
typed.MapGet("/orgs/{id}", (OrganisationId id) => Results.Text(id.LongFormat()));

const string testBaseUrl = "http://127.0.0.1:5454";

// Start in background, wait briefly, fire requests, then stop
_ = Task.Run(() => app.Run(testBaseUrl));
await Task.Delay(2000);

using var http = new HttpClient();
http.BaseAddress = new Uri(testBaseUrl);

var validResp = await http.GetAsync(new Uri("/customers/189001019802", UriKind.Relative));
if (!validResp.IsSuccessStatusCode)
    throw new InvalidOperationException($"Expected 200, got {(int)validResp.StatusCode}");
Console.WriteLine("  ✓ Valid PersonalId route → 200");

var orgResp = await http.GetAsync(new Uri("/orgs/5560160680", UriKind.Relative));
if (!orgResp.IsSuccessStatusCode)
    throw new InvalidOperationException($"Expected 200 for org, got {(int)orgResp.StatusCode}");
Console.WriteLine("  ✓ Valid OrganisationId route → 200");

var badResp = await http.GetAsync(new Uri("/customers/19811218bad9", UriKind.Relative));
if ((int)badResp.StatusCode != 400)
    throw new InvalidOperationException($"Expected 400, got {(int)badResp.StatusCode}");
Console.WriteLine("  ✓ Malformed PersonalId route → 400");

// OpenAPI document
var openApiResp = await http.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));
if (!openApiResp.IsSuccessStatusCode)
    throw new InvalidOperationException($"Expected 200 from OpenAPI doc, got {(int)openApiResp.StatusCode}");
var openApiRaw = await openApiResp.Content.ReadAsStringAsync();
var openApiBody = JsonNode.Parse(openApiRaw);
if (openApiBody?["paths"] is null)
    throw new InvalidOperationException("OpenAPI doc missing 'paths' node");
Console.WriteLine("  ✓ OpenAPI document available");

await app.StopAsync();

// ── 6. Caveat 3 probe — Mvc.JsonOptions setup registered without AddControllers ───
// AddCivitasIdSwedenAspNetCore() above registers BOTH IConfigureOptions<Http.Json.JsonOptions>
// AND IConfigureOptions<Mvc.JsonOptions>. The MVC option is a silent no-op when AddControllers
// is not invoked (Mvc.JsonOptions is never resolved by the framework without the MVC pipeline).
// This section asserts that the registration itself does not trip IL2026/IL3050 trimming
// warnings under PublishAot=true — that's verified at publish time by the surrounding script.
var mvcJsonSetupRegistered = builder.Services.Any(d =>
    d.ServiceType == typeof(Microsoft.Extensions.Options.IConfigureOptions<Microsoft.AspNetCore.Mvc.JsonOptions>)
    && d.ImplementationType?.FullName == "Civitas.Id.Sweden.AspNetCore.Json.CivitasIdMvcJsonOptionsSetup");
if (!mvcJsonSetupRegistered)
    throw new InvalidOperationException(
        "Expected IConfigureOptions<Mvc.JsonOptions> -> CivitasIdMvcJsonOptionsSetup to be registered by AddCivitasIdSwedenAspNetCore.");
Console.WriteLine("  ✓ Mvc.JsonOptions setup registered (Caveat 3 — AOT-clean without AddControllers)");

Console.WriteLine("=== AOT smoke test PASS ===");
return 0;

internal static class DataAnnotationsProbe
{
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:RequiresUnreferencedCode",
        Justification = "AOT smoke test uses ValidationContext without a display name; reflection target (DisplayNameAttribute) is not required for this probe.")]
    public static void Run()
    {
        var attr = new ValidPersonalIdAttribute();
        var ctx = new ValidationContext(new object());

        var ok = attr.GetValidationResult("189001019802", ctx);
        if (ok != ValidationResult.Success)
            throw new InvalidOperationException($"Expected success, got: {ok?.ErrorMessage}");

        var bad = attr.GetValidationResult("not-a-pnr", ctx);
        if (bad == ValidationResult.Success)
            throw new InvalidOperationException("Expected failure on malformed input");
    }
}

internal static class TypeConverterProbe
{
    // PersonalIdTypeConverter is declared in the same assembly as PersonalId via
    // [TypeConverter] attribute. The discovery path goes through reflection but the
    // converter type is preserved by the [TypeConverter(typeof(...))] attribute,
    // so this is AOT-safe in practice. We suppress the analyzer warning to acknowledge
    // we've verified the reachability.
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:RequiresUnreferencedCode",
        Justification = "PersonalIdTypeConverter is referenced via [TypeConverter] on PersonalId; preserved by trimming.")]
    public static PersonalId RoundTrip(string input)
    {
        var conv = TypeDescriptor.GetConverter(typeof(PersonalId));
        if (conv.GetType().Name != "PersonalIdTypeConverter")
            throw new InvalidOperationException($"Expected PersonalIdTypeConverter, got {conv.GetType().Name}");
        return (PersonalId)conv.ConvertFromString(input)!;
    }
}

internal static class Assert
{
    public static void Equal<T>(T actual, T expected, string label)
    {
        if (!EqualityComparer<T>.Default.Equals(actual, expected))
            throw new InvalidOperationException(
                $"AOT smoke test FAIL @ {label}: expected {expected}, got {actual}");
    }
}
