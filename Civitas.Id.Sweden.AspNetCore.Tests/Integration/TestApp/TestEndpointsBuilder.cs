using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Format;
using Microsoft.AspNetCore.Mvc;

namespace Civitas.Id.Sweden.AspNetCore.Tests.Integration.TestApp;

/// <summary>
/// Centralizes minimal-API test-endpoint registration for
/// <see cref="IntegrationTestFixture"/>. Extracted per follow-up #38 to
/// keep the fixture body legible as the integration-test surface grows.
///
/// <para>
/// Each endpoint group below documents which test class(es) consume it.
/// When adding a new endpoint, cite the consuming test class so future
/// readers can trace endpoint → assertion without grepping the test tree.
/// </para>
///
/// <list type="bullet">
/// <item><b>/customers/{id}</b> (GET) — typed-route binding. Consumed by
/// <c>ResponseShapeTests</c>, <c>CustomizeProblemDetailsTests</c>,
/// <c>ProblemDetailsSnapshotTests</c>, <c>ErrorPathTests</c>.</item>
///
/// <item><b>/lookup</b> (GET, query) — typed-query binding. Consumed by
/// <c>RouteBindingTests</c> and friends.</item>
///
/// <item><b>/customers</b> (POST, body) — typed top-level body
/// deserialization through the chained <c>TypeInfoResolver</c>.</item>
///
/// <item><b>/any/{id}</b> (GET) — polymorphic <see cref="SwedishOfficialId"/>
/// parsing inside the handler (the sum-type base does not implement
/// <c>IParsable&lt;T&gt;</c>).</item>
///
/// <item><b>/orgs/{id}</b> (GET) — typed-route binding for
/// <see cref="OrganisationId"/>. Consumed by <c>ErrorPathTests</c>.</item>
///
/// <item><b>/customers-strict/{idString}</b> (GET) — forces the explicit
/// <c>Parse</c> throw → <c>IExceptionHandler</c> path. Used by
/// <c>ErrorPathTests</c> to assert the <c>ProblemDetails</c> body emitted by
/// <c>InvalidIdNumberExceptionHandler</c>.</item>
///
/// <item><b>/consumer-400-probe</b> (GET) — emits a 400 with a non-library
/// <c>type</c> URI. Consumed by <c>CustomizeProblemDetailsTests</c> to verify
/// <c>CustomizeProblemDetails</c> preserves consumer <c>type</c> values.</item>
///
/// <item><b>/internal-error-probe</b> (GET) — emits a 500. Consumed by
/// <c>CustomizeProblemDetailsTests</c> to verify the non-400 early-return
/// branch (only 400s are normalized).</item>
///
/// <item><b>/consumer-422-probe</b> (GET) — emits a 422 with a non-library
/// <c>type</c> URI. Consumed by <c>CustomizeProblemDetailsTests</c> to verify
/// the guard scope (only 400s are normalized).</item>
///
/// <item><b>/orgs-strict/{idString}</b> (GET) — org equivalent of
/// <c>/customers-strict</c>. Consumed by <c>ProblemDetailsSnapshotTests</c>
/// for <see cref="OrganisationId"/> parse-failure shapes.</item>
/// </list>
/// </summary>
internal static class TestEndpointsBuilder
{
    /// <summary>
    /// Maps the full set of test endpoints onto the supplied
    /// <see cref="IEndpointRouteBuilder"/>. The caller is expected to have
    /// applied <c>WithCivitasIdSwedenMetadata()</c> (or equivalent) at the
    /// group level when typed-ID metadata is desired.
    /// </summary>
    public static IEndpointRouteBuilder MapTestEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        // -- Typed-route + typed-query + typed-body --------------------------
        _ = endpoints.MapGet("/customers/{id}", (PersonalId id) =>
            Results.Ok(new CustomerResponse(id, id.GetAge(), id.IsAdult())));

        _ = endpoints.MapGet("/lookup", (PersonalId id) =>
            Results.Ok(new LookupResponse(id)));

        // Body deserialization of a top-level PersonalId works through the
        // chained TypeInfoResolver. Wrapping in a DTO with a typed property
        // relies on STJ resolving converter-per-property; that pathway
        // requires either [JsonConverter] on the type or converter
        // registration on the serializer options. The integration tests
        // cover the top-level body path, which is what the AspNetCore
        // extension actually wires up.
        _ = endpoints.MapPost("/customers", ([FromBody] PersonalId id) =>
            Results.Created($"/customers/{id}", new LookupResponse(id)));

        // Polymorphic SwedishOfficialId: parse manually inside the handler
        // since SwedishOfficialId does not implement IParsable<T> (sum-type
        // base).
        _ = endpoints.MapGet("/any/{id}", (string id) =>
        {
            var parsed = SwedishOfficialId.ParseAny(id);
            return Results.Ok(new AnyResponse(parsed.GetType().Name, parsed));
        });

        _ = endpoints.MapGet("/orgs/{id}", (OrganisationId id) => Results.Ok(new OrgResponse(
            id,
            id.Form,
            id.NumberType == OrganisationNumberType.PhysicalPerson)));

        // -- Strict (manual Parse) — forces IExceptionHandler path ----------
        _ = endpoints.MapGet("/customers-strict/{idString}", (string idString) =>
        {
            var id = PersonalId.Parse(idString);
            return Results.Ok(new LookupResponse(id));
        });

        _ = endpoints.MapGet("/orgs-strict/{idString}", (string idString) =>
        {
            var id = OrganisationId.Parse(idString);
            return Results.Ok(new OrgResponse(id, id.Form, id.NumberType == OrganisationNumberType.PhysicalPerson));
        });

        // -- CustomizeProblemDetails guard-scope probes ---------------------
        _ = endpoints.MapGet("/consumer-400-probe", () =>
            Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                type: "https://consumer.example/errors/payment",
                title: "Consumer error"));

        _ = endpoints.MapGet("/internal-error-probe", () =>
            Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                type: "https://internal/server-error",
                title: "Internal"));

        _ = endpoints.MapGet("/consumer-422-probe", () =>
            Results.Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                type: "https://consumer.example/errors/unprocessable",
                title: "Unprocessable"));

        return endpoints;
    }
}
