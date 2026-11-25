using System.Net;
using System.Net.Mime;
using System.Text;
using Civitas.Id.Sweden.AspNetCore.ExceptionHandlers;
using Civitas.Id.Sweden.AspNetCore.Extensions;
using Civitas.Id.Sweden.Core;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using static Civitas.Id.Sweden.AspNetCore.Tests.Integration.TestApp.ProblemProbeHost;
using static Civitas.Id.Sweden.AspNetCore.Tests.Integration.TestApp.ProblemTestConstants;

namespace Civitas.Id.Sweden.AspNetCore.Tests.Integration;

/// <summary>
/// A malformed ID parameter gives the same 400 in Development as in Production, where
/// ASP.NET Core alone answers 500. An app's own selector, handler or handling path keeps its result.
/// </summary>
public class TypedParameterStatusTests
{
    private const string AppErrorBody = "app-error";
    private const string AppErrorPath = "/app-error";
    private const string BodyPath = "/body";

    public static IEnumerable<string> EnvironmentNames() => [Environments.Development, Environments.Production];

    public static IEnumerable<string> MalformedRequests() =>
    [
        "/personal/" + NotAnId,
        "/coordination/" + NotAnId,
        "/organisation/" + NotAnId,
        "/query?id=" + NotAnId,
        "/query",
        "/optional?id=" + NotAnId,
        "/many?ids=" + NotAnId,
        "/bag?id=" + NotAnId
    ];

    [Test]
    [MethodDataSource(nameof(MalformedRequests))]
    public async Task Malformed_InDevelopment_AnswersAsInProduction(string path)
    {
        var development = await GetAsync(Environments.Development, path);
        var production = await GetAsync(Environments.Production, path);

        await Assert.That(development.Status).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(development.MediaType).IsEqualTo(MediaTypeNames.Application.ProblemJson);
        await Assert.That(development.Body).DoesNotContain(NotAnId);
        await Assert.That(development.Json[TypeKey]?.GetValue<string>()).IsEqualTo(LibraryBadRequestType);
        await Assert.That(development.Json.ContainsKey(TraceIdKey)).IsTrue();

        await Assert.That(production.Status).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(development.WithoutTraceId).IsEqualTo(production.WithoutTraceId);
    }

    [Test]
    [MethodDataSource(nameof(EnvironmentNames))]
    public async Task Valid_Gives200(string environment)
    {
        var response = await GetAsync(environment, "/personal/" + ValidId());

        await Assert.That(response.Status).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    [MethodDataSource(nameof(EnvironmentNames))]
    public async Task GeneratedNumber_ValidGives200_AndEveryDefect400(string environment)
    {
        var valid = await GetUnloggedAsync(environment, GeneratedNumbers.ValidPersonnummer).ConfigureAwait(false);
        await Assert.That(valid.Status).IsEqualTo(HttpStatusCode.OK);

        foreach (var text in GeneratedNumbers.InvalidPersonnummer())
        {
            var response = await GetUnloggedAsync(environment, text).ConfigureAwait(false);
            await Assert.That(response.Status).IsEqualTo(HttpStatusCode.BadRequest);
        }
    }

    [Test]
    [MethodDataSource(nameof(EnvironmentNames))]
    public async Task OrdinaryException_Stays500(string environment)
    {
        var response = await GetAsync(environment, "/boom/" + ValidId());

        await Assert.That(response.Status).IsEqualTo(HttpStatusCode.InternalServerError);
    }

    /// <summary>The exception's own status is used, whichever it is and whoever threw it.</summary>
    [Test]
    [MethodDataSource(nameof(EnvironmentNames))]
    public async Task BadHttpRequestException_AnswersWithItsOwnStatus(string environment)
    {
        var response = await GetAsync(environment, "/toolarge/" + ValidId());

        await Assert.That(response.Status).IsEqualTo(HttpStatusCode.RequestEntityTooLarge);
    }

    [Test]
    public async Task EndpointWithoutId_InDevelopment_AlsoGives400()
    {
        var response = await GetAsync(Environments.Development, "/number?n=" + NotAnId);

        await Assert.That(response.Status).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task AppSelector_IsKept()
    {
        var response = await GetAsync(Environments.Development, "/personal/" + NotAnId, static services =>
            services.Configure<ExceptionHandlerOptions>(static o =>
                o.StatusCodeSelector = static _ => StatusCodes.Status418ImATeapot));

        await Assert.That((int)response.Status).IsEqualTo(StatusCodes.Status418ImATeapot);
    }

    [Test]
    public async Task AppExceptionHandler_IsKept()
    {
        var response = await GetAsync(Environments.Development, "/personal/" + NotAnId, static services =>
            services.Configure<ExceptionHandlerOptions>(static o =>
                o.ExceptionHandler = static context => context.Response.WriteAsync(AppErrorBody)));

        await Assert.That(response.Status).IsEqualTo(HttpStatusCode.InternalServerError);
        await Assert.That(response.Body).IsEqualTo(AppErrorBody);
    }

    [Test]
    public async Task AppSettingsPassedDirectly_AreKept()
    {
        var response = await GetAsync(Environments.Development, "/personal/" + NotAnId, use: static app =>
            app.UseExceptionHandler(AppErrorPath));

        await Assert.That(response.Status).IsEqualTo(HttpStatusCode.InternalServerError);
        await Assert.That(response.Body).IsEqualTo(AppErrorBody);
    }

    /// <summary>
    /// The framework's check for a missing error page still works: it rethrows. In Production and
    /// without <c>UseStatusCodePages</c>, because the developer exception page would catch the
    /// rethrow and status-code pages would answer the 404 first.
    /// </summary>
    [Test]
    public async Task MissingErrorPage_StillRethrows()
    {
        await Assert.That(static async () =>
            {
                var setup = new TestHostSetup(Environments.Production)
                {
                    Configure = static services => services
                        .Configure<RouteHandlerOptions>(static o => o.ThrowOnBadRequest = true)
                        .Configure<ExceptionHandlerOptions>(static o => o.ExceptionHandlingPath = "/no-such-page"),
                    Use = static app => app.UseExceptionHandler()
                };
                await GetAsync(setup, "/personal/" + NotAnId);
            })
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task AppExceptionHandlerService_StartsFromTheExceptionsStatus()
    {
        var response = await GetAsync(Environments.Development, "/personal/" + NotAnId, static services =>
            services.AddSingleton<IExceptionHandler>(new WritingHandler()));

        await Assert.That(response.Status).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(response.Body).IsEqualTo(AppErrorBody);
    }

    [Test]
    public async Task MalformedIdInBody_InDevelopment_AnswersAsInProduction()
    {
        var development = await PostMalformedBodyAsync(Environments.Development);
        var production = await PostMalformedBodyAsync(Environments.Production);

        await Assert.That(development.Status).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(development.MediaType).IsEqualTo(MediaTypeNames.Application.ProblemJson);
        await Assert.That(development.Body).DoesNotContain(NotAnId);
        await Assert.That(development.Json["status"]?.GetValue<int>()).IsEqualTo(StatusCodes.Status400BadRequest);
        await Assert.That(development.WithoutTraceId).IsEqualTo(production.WithoutTraceId);
    }

    [Test]
    public async Task AppHandlingPath_IsKept()
    {
        var response = await GetAsync(Environments.Development, "/personal/" + NotAnId, static services =>
            services.Configure<ExceptionHandlerOptions>(static o => o.ExceptionHandlingPath = AppErrorPath));

        await Assert.That(response.Status).IsEqualTo(HttpStatusCode.InternalServerError);
        await Assert.That(response.Body).IsEqualTo(AppErrorBody);
    }

    [Test]
    public async Task ThrowingInProduction_AlsoGives400()
    {
        var response = await GetAsync(Environments.Production, "/personal/" + NotAnId, static services =>
            services.Configure<RouteHandlerOptions>(static o => o.ThrowOnBadRequest = true));

        await Assert.That(response.Status).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(response.Body).DoesNotContain(NotAnId);
    }

    [Test]
    public async Task RegisteringTwice_AddsOneSetup()
    {
        var services = new ServiceCollection();
        services.AddCivitasIdSwedenAspNetCore();
        services.AddCivitasIdSwedenAspNetCore();

        var setups = services.Count(static d => d.ImplementationType == typeof(BadRequestStatusSetup));

        await Assert.That(setups).IsEqualTo(1);
    }

    [Test]
    public async Task PostConfigure_NullOptions_Throws()
    {
        await Assert.That(static () => new BadRequestStatusSetup().PostConfigure(null, null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    private static string ValidId() => OfficialNumbers.Modern.Long;

    private static Task<ProbeResponse> GetAsync(
        string environment, string path, Action<IServiceCollection>? configure = null)
        => GetAsync(new TestHostSetup(environment) { Configure = configure }, path);

    /// <summary>Without logging, because the host logs the request path, which holds the number.</summary>
    private static Task<ProbeResponse> GetUnloggedAsync(string environment, string number)
        => GetAsync(environment, "/personal/" + number, static services =>
            services.AddLogging(static logging => logging.ClearProviders()));

    private static Task<ProbeResponse> GetAsync(string environment, string path, Action<WebApplication> use)
        => GetAsync(new TestHostSetup(environment) { Use = use }, path);

    private static async Task<ProbeResponse> GetAsync(TestHostSetup setup, string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(path, UriKind.Relative));
        return await SendAsync(setup, request);
    }

    private static async Task<ProbeResponse> PostMalformedBodyAsync(string environment)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(BodyPath, UriKind.Relative));
        request.Content = new StringContent($"\"{NotAnId}\"", Encoding.UTF8, MediaTypeNames.Application.Json);
        return await SendAsync(new TestHostSetup(environment), request);
    }

    private static async Task<ProbeResponse> SendAsync(TestHostSetup setup, HttpRequestMessage request)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = setup.Environment });
        builder.WebHost.UseTestServer();
        builder.Services.AddCivitasIdSwedenAspNetCore();
        setup.Configure?.Invoke(builder.Services);
        await using var app = builder.Build();
        setup.Use(app);
        MapEndpoints(app);
        await app.StartAsync();

        using var client = app.GetTestClient();
        using var response = await client.SendAsync(request);
        return new ProbeResponse(response.StatusCode, await response.Content.ReadAsStringAsync())
        {
            MediaType = response.Content.Headers.ContentType?.MediaType
        };
    }

    private static void MapEndpoints(WebApplication app)
    {
        app.MapGet("/personal/{id}", (PersonalId id) => id.LongFormat());
        app.MapGet("/coordination/{id}", (CoordinationId id) => id.LongFormat());
        app.MapGet("/organisation/{id}", (OrganisationId id) => id.LongFormat());
        app.MapGet("/query", (PersonalId id) => id.LongFormat());
        app.MapGet("/optional", (PersonalId? id) => id?.LongFormat() ?? string.Empty);
        app.MapGet("/many", (PersonalId[] ids) => ids.Length);
        app.MapGet("/bag", ([AsParameters] Bag bag) => bag.Id.LongFormat());
        app.MapPost(BodyPath, ([FromBody] PersonalId id) => id.LongFormat());
        app.MapGet("/number", (int n) => n);
        app.MapGet("/boom/{id}", string (PersonalId id) => throw new InvalidOperationException(id.GetType().Name));
        app.MapGet("/toolarge/{id}", string (PersonalId id) =>
            throw new BadHttpRequestException(id.GetType().Name, StatusCodes.Status413PayloadTooLarge));
        app.MapGet(AppErrorPath, () => AppErrorBody);
    }

    private sealed record TestHostSetup(string Environment)
    {
        public Action<IServiceCollection>? Configure { get; init; }

        public Action<WebApplication> Use { get; init; } = static app =>
        {
            app.UseExceptionHandler();
            app.UseStatusCodePages();
        };
    }

    private sealed class WritingHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            await httpContext.Response.WriteAsync(AppErrorBody, cancellationToken);
            return true;
        }
    }

    private readonly record struct Bag(PersonalId Id);
}
