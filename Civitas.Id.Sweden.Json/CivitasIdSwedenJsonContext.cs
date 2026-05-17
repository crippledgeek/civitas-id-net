using System.Text.Json.Serialization;
using Civitas.Id.Sweden.Core;
using JetBrains.Annotations;

namespace Civitas.Id.Sweden.Json;

/// <summary>
/// Source-generated <see cref="JsonSerializerContext"/> for AOT-friendly
/// serialization of Civitas.Id.Sweden ID types.
/// </summary>
/// <remarks>
/// Consumers chain this context onto their own <see cref="System.Text.Json.JsonSerializerOptions"/>
/// via <c>options.TypeInfoResolverChain.Add(CivitasIdSwedenJsonContext.Default)</c>
/// to get AOT-safe serialization of <see cref="PersonalId"/>,
/// <see cref="CoordinationId"/>, <see cref="OrganisationId"/>, and
/// <see cref="SwedishOfficialId"/>.
/// </remarks>
[JsonSerializable(typeof(PersonalId))]
[JsonSerializable(typeof(CoordinationId))]
[JsonSerializable(typeof(OrganisationId))]
[JsonSerializable(typeof(SwedishOfficialId))]
[PublicAPI]
public partial class CivitasIdSwedenJsonContext : JsonSerializerContext;
