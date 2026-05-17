using System.Diagnostics.CodeAnalysis;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Format;

namespace Civitas.Id.Sweden.AspNetCore.Tests.Integration.TestApp;

// CA1812 + NotAccessedPositionalProperty.Global: these DTOs are instantiated
// by endpoint delegates returning typed results, and their properties are
// serialized by System.Text.Json and consumed by the test HTTP clients —
// neither construction site nor property read is visible to the analyzers.

/// <summary>Response shape for the customer GET endpoint.</summary>
[SuppressMessage("Performance", "CA1812", Justification = "Instantiated by endpoint handler.")]
[SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global", Justification = "Serialized to JSON; consumed by HTTP test clients, not direct code.")]
internal sealed record CustomerResponse(PersonalId Id, int Age, bool IsAdult);

/// <summary>Response shape for the lookup query / POST endpoints.</summary>
[SuppressMessage("Performance", "CA1812", Justification = "Instantiated by endpoint handler.")]
[SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global", Justification = "Serialized to JSON; consumed by HTTP test clients, not direct code.")]
internal sealed record LookupResponse(PersonalId Id);

/// <summary>Response shape for the polymorphic /any endpoint.</summary>
[SuppressMessage("Performance", "CA1812", Justification = "Instantiated by endpoint handler.")]
[SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global", Justification = "Serialized to JSON; consumed by HTTP test clients, not direct code.")]
internal sealed record AnyResponse(string Type, SwedishOfficialId Id);

/// <summary>Response shape for the organisation GET endpoint.</summary>
[SuppressMessage("Performance", "CA1812", Justification = "Instantiated by endpoint handler.")]
[SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global", Justification = "Serialized to JSON; consumed by HTTP test clients, not direct code.")]
internal sealed record OrgResponse(OrganisationId Id, OrganisationForm Form, bool IsPhysical);
