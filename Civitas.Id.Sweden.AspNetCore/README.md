# Civitas.Id.Sweden.AspNetCore

ASP.NET Core integration for `Civitas.Id.Sweden`:

- Minimal-API route-binding via `IParsable<T>` for `PersonalId`, `CoordinationId`, `OrganisationId`.
- MVC controller binding through the same parsers + DataAnnotations validators.
- System.Text.Json converter wiring on both `Microsoft.AspNetCore.Http.Json.JsonOptions` and `Microsoft.AspNetCore.Mvc.JsonOptions`.
- `IExceptionHandler` adapter that turns `InvalidIdNumberException` into RFC 7807 `ProblemDetails`.
- OpenAPI schema transformer for the three ID types (opt-in via `AddCivitasIdSwedenSchemas` on `OpenApiOptions`).
- Startup-time validator (`IHostedService`) that fails fast if `AddProblemDetails()` was not called.

## Getting started

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCivitasIdSwedenAspNetCore();           // converters, exception handler, ProblemDetails
builder.Services.AddControllers().AddCivitasIdSweden();    // MVC JSON bridge (only if you use MVC)
builder.Services.AddOpenApi(o => o.AddCivitasIdSwedenSchemas()); // opt-in OpenAPI schemas

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.MapOpenApi();
app.MapControllers();
app.MapGet("/customers/{id}", (PersonalId id) => Results.Ok(id));
app.Run();
```

## Response shapes

When the library returns a 400 for an invalid Swedish ID, the response body shape depends on which surface triggered the failure. All four shapes share the same `type` URI (set by `ProblemDetailsTypeBaseUri`); only the body structure differs.

| # | Trigger | Body type | Has `errors` dict |
|---|---|---|---|
| 1 | Minimal API route param `IParsable<T>` parse fail | `ProblemDetails` | No |
| 2 | Minimal API endpoint throws `InvalidIdNumberException` | `ProblemDetails` (+ `reason` / `input` extensions) | No |
| 3 | MVC `[ApiController]` ModelState invalid via `[ValidPersonalId]` | `ValidationProblemDetails` | Yes |
| 4 | MVC `[FromBody]` STJ parse fail (typed `PersonalId` property) | `ProblemDetails` (+ `reason` / `input` extensions) | No |

Row 1 surfaces through ASP.NET Core's status-code 400 path, which the library customizes via `CustomizeProblemDetails` to set the `type` URI to `{ProblemDetailsTypeBaseUri}bad-request`. Although [`aspnetcore#62066`](https://github.com/dotnet/aspnetcore/pull/62066) added `HttpValidationProblemDetails` for minimal-API endpoint-filter validation in .NET 10 GA, typed-route-parameter parse failures for `IParsable<T>` bindings do not go through that filter — they short-circuit to a 400 from the binding layer with no field-level `errors` dictionary.

Rows 2 and 4 share the `InvalidIdNumberException` handler path, which writes `reason` and (redacted) `input` extensions onto the `ProblemDetails`.

### Example responses

Row 1 — minimal API malformed route param:

```http
GET /customers/19811218bad9 HTTP/1.1
```

Returns 400 with body:

```json
{
  "type": "https://civitas-id.dev/errors/bad-request",
  "title": "Bad Request",
  "status": 400,
  "traceId": "00-..."
}
```

Row 2 — minimal API throw via the exception handler:

```http
GET /throwing-endpoint?id=198112180000 HTTP/1.1
```

Returns 400 with body:

```json
{
  "type": "https://civitas-id.dev/errors/invalid-id-number",
  "title": "Invalid Swedish ID number",
  "status": 400,
  "detail": "The provided value is not a valid Swedish official ID.",
  "reason": "InvalidChecksum",
  "input": "********0000"
}
```

Row 3 — MVC `[ApiController]` ModelState failure:

```http
POST /legacy HTTP/1.1
Content-Type: application/json

{"id": "19811218bad9"}
```

Returns 400 with body:

```json
{
  "type": "https://civitas-id.dev/errors/bad-request",
  "title": "Bad Request",
  "status": 400,
  "errors": { "Id": ["..."] }
}
```

Row 4 — MVC `[FromBody]` JSON parse failure (when a typed `PersonalId` property fails to deserialize):

```http
POST /customer-create HTTP/1.1
Content-Type: application/json

{"id": "19811218bad9"}
```

Returns 400 with body:

```json
{
  "type": "https://civitas-id.dev/errors/invalid-id-number",
  "title": "Invalid Swedish ID number",
  "status": 400,
  "detail": "The provided value is not a valid Swedish official ID.",
  "reason": "InvalidFormat",
  "input": "********bad9"
}
```

## Consumer requirements

- Consumer csproj must add `<InterceptorsNamespaces>$(InterceptorsNamespaces);Microsoft.AspNetCore.OpenApi.Generated</InterceptorsNamespaces>` when using `MapOpenApi` with this library's schema transformer. The XML-comment source generator for OpenAPI emits interceptors in this namespace; without the opt-in entry the generated code fails to compile.
- Consumers using `[FromBody]` DTOs with typed `PersonalId` properties on the **client side** must chain `CivitasIdSwedenJsonContext.Default` into their `HttpClient`'s `JsonSerializerOptions.TypeInfoResolverChain`, otherwise `PostAsJsonAsync` will serialize the records as objects instead of canonical strings.

## See also

- Top-level [README](../README.md) — core parser/formatter library overview.
- [`Civitas.Id.Sweden.DataAnnotations`](../Civitas.Id.Sweden.DataAnnotations) — `[ValidPersonalId]` / `[ValidCoordinationId]` / `[ValidOrganisationId]` attributes.
- [`Civitas.Id.Sweden.Json`](../Civitas.Id.Sweden.Json) — STJ converters and the source-generated `JsonSerializerContext`.
