using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;

namespace Civitas.Id.Sweden.AspNetCore.Json;

/// <summary>
/// Source-generated <see cref="JsonSerializerContext"/> for ASP.NET Core-flavored
/// types used by Civitas.Id.Sweden's exception-handling and ProblemDetails paths.
/// Chained into the consumer's TypeInfoResolverChain by
/// <see cref="ConverterInstaller"/> so <c>IProblemDetailsService.TryWriteAsync</c>
/// can serialize <see cref="ProblemDetails"/> under PublishAot.
/// </summary>
[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(Microsoft.AspNetCore.Http.HttpValidationProblemDetails))]
[JsonSerializable(typeof(ValidationProblemDetails))]
[PublicAPI]
public partial class CivitasIdAspNetCoreJsonContext : JsonSerializerContext;
