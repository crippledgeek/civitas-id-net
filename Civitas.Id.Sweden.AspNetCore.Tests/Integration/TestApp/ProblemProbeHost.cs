using System.Net;
using System.Net.Mime;
using System.Text.Json.Nodes;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Errors;
using Microsoft.AspNetCore.TestHost;

namespace Civitas.Id.Sweden.AspNetCore.Tests.Integration.TestApp;

/// <summary>A host with one endpoint per way of answering with ProblemDetails.</summary>
internal static class ProblemProbeHost
{
    internal const string NotFoundPath = "/not-found";
    internal const string ThrowPath = "/throw";
    internal const string MalformedIdPath = PersonalPath + ProblemTestConstants.NotAnId;
    internal const string MalformedNumberPath = NumberPath + "?n=" + ProblemTestConstants.NotAnId;
    internal const string MvcPath = "/" + ProblemProbeController.Route + "/";

    private const string PersonalPath = "/personal/";
    private const string NumberPath = "/number";

    internal static async Task<ProbeResponse> SendAsync(ProbeRequest probe, Action<IServiceCollection> configure)
    {
        var builder = WebApplication.CreateBuilder(
            new WebApplicationOptions { EnvironmentName = probe.Environment });
        builder.WebHost.UseTestServer();
        configure(builder.Services);
        builder.Services.AddControllers().AddApplicationPart(typeof(ProblemProbeController).Assembly);
        await using var app = builder.Build();

        // The test client gives no response once an exception escapes, so the exception stops
        // here and the bytes written before it can be read.
        Exception? escaped = null;
        app.Use(async (context, next) =>
        {
            try
            {
                await next(context);
            }
            catch (Exception exception) when (exception is FormatException or InvalidOperationException or NotSupportedException)
            {
                escaped = exception;
            }
        });
        if (probe.UseExceptionHandler)
        {
            app.UseExceptionHandler();
        }

        app.UseStatusCodePages();
        app.MapControllers();
        app.MapGet(PersonalPath + "{id}", (PersonalId id) => id.LongFormat());
        app.MapGet(NumberPath, (int n) => n);
        app.MapGet(NotFoundPath, () => Results.Problem(statusCode: StatusCodes.Status404NotFound));
        app.MapGet(ThrowPath, string () =>
            throw new InvalidIdNumberException(ProblemTestConstants.NotAnId, InvalidIdNumberReason.InvalidFormat));
        await app.StartAsync();

        using var client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(probe.Path, UriKind.Relative));
        request.Headers.Accept.ParseAdd(probe.Accept);
        using var response = await client.SendAsync(request);
        return new ProbeResponse(response.StatusCode, await response.Content.ReadAsStringAsync())
        {
            MediaType = response.Content.Headers.ContentType?.MediaType,
            Escaped = escaped
        };
    }

    /// <summary>What to ask the host.</summary>
    internal sealed record ProbeRequest(string Path)
    {
        public string Accept { get; init; } = MediaTypeNames.Application.Json;

        public string Environment { get; init; } = Environments.Production;

        public bool UseExceptionHandler { get; init; }
    }

    /// <summary>What the host answered, and the exception that left its pipeline.</summary>
    internal sealed record ProbeResponse(HttpStatusCode Status, string Body)
    {
        public string? MediaType { get; init; }

        public Exception? Escaped { get; init; }

        public JsonObject Json => JsonNode.Parse(Body)!.AsObject();

        public string WithoutTraceId
        {
            get
            {
                var json = Json;
                json.Remove(ProblemTestConstants.TraceIdKey);
                return json.ToJsonString();
            }
        }
    }
}
