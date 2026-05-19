using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using HttpJsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

namespace Civitas.Id.Sweden.AspNetCore.Hosting;

/// <summary>
/// Validates at app startup that <see cref="ProblemDetails"/> is reachable
/// through the <c>Microsoft.AspNetCore.Http.Json.JsonOptions</c>
/// <c>TypeInfoResolverChain</c>. Fails fast with actionable guidance rather
/// than letting <c>IProblemDetailsService.TryWriteAsync</c> throw
/// <see cref="NotSupportedException"/> on the first 400 response.
/// </summary>
internal sealed class CivitasIdSwedenStartupValidator(IOptions<HttpJsonOptions> httpJsonOptions) : IHostedService
{
    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var serializer = httpJsonOptions.Value.SerializerOptions;
        if (!serializer.TryGetTypeInfo(typeof(ProblemDetails), out _))
        {
            throw new InvalidOperationException(
                "Civitas.Id.Sweden.AspNetCore startup check: ProblemDetails is not reachable through " +
                "Microsoft.AspNetCore.Http.Json.JsonOptions.SerializerOptions.TypeInfoResolverChain. " +
                "Either: (a) add a JsonSerializerContext that includes [JsonSerializable(typeof(ProblemDetails))] " +
                "and chain it into JsonOptions.SerializerOptions.TypeInfoResolverChain.Add(YourContext.Default), " +
                "or (b) ensure services.AddCivitasIdSwedenAspNetCore() is called (it registers the necessary " +
                "AddProblemDetails configuration automatically).");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
