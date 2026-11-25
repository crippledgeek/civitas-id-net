using System.Text.Json.Nodes;
using Civitas.Id.Sweden.AspNetCore.ExceptionHandlers;
using Civitas.Id.Sweden.AspNetCore.Extensions;
using Civitas.Id.Sweden.AspNetCore.Options;
using Civitas.Id.Sweden.AspNetCore.Tests.Integration.TestApp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Options;
using static Civitas.Id.Sweden.AspNetCore.Tests.Integration.TestApp.ProblemTestConstants;

namespace Civitas.Id.Sweden.AspNetCore.Tests.Integration;

/// <summary>
/// The library's and the app's <c>CustomizeProblemDetails</c> both run, whichever was registered
/// first, and the app has the last word.
/// </summary>
public class ProblemDetailsChainTests
{
    private const string CustomBaseUri = "https://errors.example/";
    private const string CustomBadRequestType = CustomBaseUri + "bad-request";

    /// <summary>Whether the app registers its callback before or after the library.</summary>
    public enum Order
    {
        AppFirst,
        AppLast
    }

    public static IEnumerable<string> BadRequestPaths() =>
    [
        ProblemProbeHost.MalformedIdPath,
        ProblemProbeHost.MalformedNumberPath,
        ProblemProbeHost.MvcPath + ProblemProbeController.BadRequestAction,
        ProblemProbeHost.MvcPath + ProblemProbeController.ValidatedAction + "?number=" + NotAnId
    ];

    public static IEnumerable<(string Path, Order Order)> BadRequestCases() => InBothOrders(BadRequestPaths());

    public static IEnumerable<(string Path, Order Order)> NotFoundCases() => InBothOrders(
        [ProblemProbeHost.NotFoundPath, ProblemProbeHost.MvcPath + ProblemProbeController.NotFoundAction]);

    [Test]
    [MethodDataSource(nameof(BadRequestCases))]
    public async Task BadRequest_BothRun(string path, Order order)
    {
        var problem = await GetAsync(path, services => Register(services, order));

        await Assert.That(problem[TypeKey]?.GetValue<string>()).IsEqualTo(LibraryBadRequestType);
        await Assert.That(problem[AppKey]?.GetValue<string>()).IsEqualTo(AppValue);
    }

    [Test]
    [MethodDataSource(nameof(BadRequestPaths))]
    public async Task WithoutAppCustomization_LibraryTypeIsSet(string path)
    {
        var problem = await GetAsync(path, static services => services.AddCivitasIdSwedenAspNetCore());

        await Assert.That(problem[TypeKey]?.GetValue<string>()).IsEqualTo(LibraryBadRequestType);
        await Assert.That(problem.ContainsKey(AppKey)).IsFalse();
    }

    [Test]
    [MethodDataSource(nameof(NotFoundCases))]
    public async Task OtherStatus_KeepsItsTypeAndRunsTheApp(string path, Order order)
    {
        var problem = await GetAsync(path, services => Register(services, order));

        await Assert.That(problem[TypeKey]?.GetValue<string>()).IsEqualTo(FrameworkNotFoundType);
        await Assert.That(problem[AppKey]?.GetValue<string>()).IsEqualTo(AppValue);
    }

    [Test]
    [MethodDataSource(nameof(BadRequestPaths))]
    public async Task CustomBaseUri_ReachesTheResponse(string path)
    {
        var problem = await GetAsync(path, static services =>
            services.AddCivitasIdSwedenAspNetCore(static o => o.ProblemDetailsTypeBaseUri = CustomBaseUri));

        await Assert.That(problem[TypeKey]?.GetValue<string>()).IsEqualTo(CustomBadRequestType);
    }

    /// <summary>MVC writes the instance it created, so a callback has to edit that one.</summary>
    [Test]
    public async Task ReplacedInstance_IsNotUsedByMvc()
    {
        var problem = await GetAsync(
            ProblemProbeHost.MvcPath + ProblemProbeController.BadRequestAction,
            static services =>
            {
                services.AddProblemDetails(static o => o.CustomizeProblemDetails = static c =>
                    c.ProblemDetails = new ProblemDetails { Type = CustomBadRequestType });
                services.AddCivitasIdSwedenAspNetCore();
            });

        await Assert.That(problem[TypeKey]?.GetValue<string>()).IsEqualTo(LibraryBadRequestType);
    }

    /// <summary>The app runs last, so it can choose even the framework's own type.</summary>
    [Test]
    public async Task AppSetsFrameworkType_IsKept()
    {
        var problem = await GetAsync(ProblemProbeHost.MalformedIdPath, static services =>
        {
            services.AddProblemDetails(static o => o.CustomizeProblemDetails = static c =>
                c.ProblemDetails.Type = FrameworkBadRequestType);
            services.AddCivitasIdSwedenAspNetCore();
        });

        await Assert.That(problem[TypeKey]?.GetValue<string>()).IsEqualTo(FrameworkBadRequestType);
    }

    /// <summary>A later post-configuration that assigns replaces both: there is no later configuration step.</summary>
    [Test]
    public async Task LaterPostConfigureThatAssigns_Wins()
    {
        var problem = await GetAsync(ProblemProbeHost.MalformedIdPath, static services =>
        {
            services.AddCivitasIdSwedenAspNetCore();
            services.PostConfigure<ProblemDetailsOptions>(static o => o.CustomizeProblemDetails = Mark);
        });

        await Assert.That(problem[AppKey]?.GetValue<string>()).IsEqualTo(AppValue);
        await Assert.That(problem[TypeKey]?.GetValue<string>()).IsEqualTo(FrameworkBadRequestType);
    }

    /// <summary>No exception handler is in the pipeline, so the app's exception is the one that leaves it.</summary>
    [Test]
    public async Task ThrowingAppCustomization_SurfacesItsException()
    {
        var response = await ProblemProbeHost.SendAsync(
            new ProblemProbeHost.ProbeRequest(ProblemProbeHost.MalformedIdPath),
            static services =>
            {
                services.AddProblemDetails(static o => o.CustomizeProblemDetails = static _ =>
                    throw new InvalidOperationException(AppKey));
                services.AddCivitasIdSwedenAspNetCore();
            });

        await Assert.That(response.Escaped).IsTypeOf<InvalidOperationException>();
        await Assert.That(response.Escaped!.Message).IsEqualTo(AppKey);
        await Assert.That(response.Body).IsEmpty();
    }

    [Test]
    public async Task RegisteringThrice_AddsOneSetup()
    {
        var services = new ServiceCollection();
        services.AddCivitasIdSwedenAspNetCore();
        services.AddCivitasIdSwedenAspNetCore();
        services.AddCivitasIdSwedenAspNetCore();

        var setups = services.Count(static d => d.ImplementationType == typeof(BadRequestTypeSetup));

        await Assert.That(setups).IsEqualTo(1);
    }

    [Test]
    public async Task PostConfigure_NullOptions_Throws()
    {
        var setup = new BadRequestTypeSetup(new OptionsWrapper<CivitasIdSwedenAspNetCoreOptions>(new CivitasIdSwedenAspNetCoreOptions()));

        await Assert.That(() => setup.PostConfigure(null, null!)).ThrowsExactly<ArgumentNullException>();
    }

    /// <summary>MVC calls the customization without an <see cref="HttpContext" /> when its caller has none.</summary>
    [Test]
    public async Task MvcFactoryWithoutHttpContext_SetsTheType()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddControllers();
        services.AddCivitasIdSwedenAspNetCore();
        await using var provider = services.BuildServiceProvider();

        var problem = provider.GetRequiredService<ProblemDetailsFactory>()
            .CreateProblemDetails(null!, StatusCodes.Status400BadRequest);

        await Assert.That(problem.Type).IsEqualTo(LibraryBadRequestType);
    }

    private static IEnumerable<(string Path, Order Order)> InBothOrders(IEnumerable<string> paths) =>
        paths.SelectMany(static _ => Enum.GetValues<Order>(), static (path, order) => (path, order));

    private static void Register(IServiceCollection services, Order order)
    {
        if (order == Order.AppLast)
        {
            services.AddCivitasIdSwedenAspNetCore();
        }

        services.AddProblemDetails(static o => o.CustomizeProblemDetails = Mark);
        if (order == Order.AppFirst)
        {
            services.AddCivitasIdSwedenAspNetCore();
        }
    }

    private static void Mark(ProblemDetailsContext context) => context.ProblemDetails.Extensions[AppKey] = AppValue;

    private static async Task<JsonObject> GetAsync(string path, Action<IServiceCollection> configure)
    {
        var response = await ProblemProbeHost.SendAsync(new ProblemProbeHost.ProbeRequest(path), configure);
        return response.Json;
    }
}
