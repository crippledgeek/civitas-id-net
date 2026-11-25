using System.Globalization;
using System.Net;
using System.Net.Mime;
using System.Text.Json.Serialization;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.AspNetCore.ExceptionHandlers;
using Civitas.Id.Sweden.AspNetCore.Extensions;
using Civitas.Id.Sweden.AspNetCore.Json;
using Civitas.Id.Sweden.AspNetCore.Options;
using Civitas.Id.Sweden.AspNetCore.Tests.Integration.TestApp;
using Civitas.Id.Sweden.Errors;
using Civitas.Id.Sweden.Format;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using static Civitas.Id.Sweden.AspNetCore.Tests.Integration.TestApp.ProblemTestConstants;
using HttpJsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

namespace Civitas.Id.Sweden.AspNetCore.Tests.Integration;

/// <summary>
/// <c>CustomizeProblemDetails</c> decides the body of an <see cref="InvalidIdNumberException" />
/// response whatever the client accepts, and its exceptions are not swallowed.
/// </summary>
public class ProblemDetailsFallbackTests
{
    private const string TitleKey = "title";
    private const string StatusKey = "status";
    private const string DetailKey = "detail";
    private const string InstanceKey = "instance";
    private const string CommaDecimalCulture = "sv-SE";
    private const string HiddenValue = "must-not-be-sent";
    private const int DerivedCode = 7;
    private const double Fraction = 1.5;
    private static readonly int[] Numbers = [1, 2];

    public static IEnumerable<string> AcceptedTypes() =>
        [MediaTypeNames.Application.Json, MediaTypeNames.Text.Plain, MediaTypeNames.Text.Html];

    /// <summary>Types for which no writer claims the response.</summary>
    public static IEnumerable<string> UnclaimedTypes() => [MediaTypeNames.Text.Plain, MediaTypeNames.Text.Html];

    public static IEnumerable<(string Accept, Func<Exception> Failure)> Failures() =>
        from accept in AcceptedTypes()
        from failure in new Func<Exception>[]
        {
            static () => new NotSupportedException(AppValue),
            static () => new InvalidOperationException(AppValue)
        }
        select (accept, failure);

    /// <summary>App customizations whose result has no JSON form.</summary>
    public static IEnumerable<(string Accept, Action<IServiceCollection> Configure)> Unserializable() =>
        from accept in AcceptedTypes()
        from configure in new[]
        {
            Customize(static c => c.ProblemDetails.Extensions[AppKey] = typeof(string)),
            SourceGeneratedOnly + Customize(static c => c.ProblemDetails.Extensions[AppKey] = new Details(AppValue, HiddenValue)),
            SourceGeneratedOnly + Customize(static c => c.ProblemDetails = new DerivedProblem())
        }
        select (accept, configure);

    [Test]
    [MethodDataSource(nameof(AcceptedTypes))]
    public async Task WithoutCustomization_WritesTheLibraryProblem(string accept)
    {
        var response = await ThrowAsync(accept, static _ => { });

        await Assert.That(response.Status).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(response.MediaType).IsEqualTo(MediaTypeNames.Application.ProblemJson);
        await Assert.That(response.Json[TypeKey]?.GetValue<string>()).IsEqualTo(LibraryInvalidIdType);
        await Assert.That(response.Json[TitleKey]?.GetValue<string>()).IsEqualTo("Invalid Swedish ID number");
        await Assert.That(response.Json[StatusKey]?.GetValue<int>()).IsEqualTo(StatusCodes.Status400BadRequest);
        await Assert.That(response.Json[DetailKey]?.GetValue<string>())
            .IsEqualTo("The provided value is not a valid Swedish official ID.");
        await Assert.That(response.Json.ContainsKey(InstanceKey)).IsFalse();
        await Assert.That(response.Json[ReasonKey]?.GetValue<string>())
            .IsEqualTo(nameof(InvalidIdNumberReason.InvalidFormat));
        await Assert.That(response.Json.ContainsKey(InputKey)).IsTrue();
        await Assert.That(response.Body).DoesNotContain(NotAnId);
    }

    [Test]
    [MethodDataSource(nameof(AcceptedTypes))]
    public async Task RemovedInput_IsNotSent(string accept)
    {
        var response = await ThrowAsync(accept, Customize(static c => c.ProblemDetails.Extensions.Remove(InputKey)));

        await Assert.That(response.Json.ContainsKey(InputKey)).IsFalse();
        await Assert.That(response.Json.ContainsKey(ReasonKey)).IsTrue();
    }

    [Test]
    [MethodDataSource(nameof(AcceptedTypes))]
    public async Task AddedExtension_IsSent(string accept)
    {
        var response = await ThrowAsync(accept, Customize(static c => c.ProblemDetails.Extensions[AppKey] = AppValue));

        await Assert.That(response.Json[AppKey]?.GetValue<string>()).IsEqualTo(AppValue);
    }

    [Test]
    [MethodDataSource(nameof(AcceptedTypes))]
    public async Task ReplacedProblem_IsSent(string accept)
    {
        var response = await ThrowAsync(accept, Customize(static c => c.ProblemDetails = new ProblemDetails
        {
            Type = FrameworkNotFoundType,
            Title = AppValue,
            Status = StatusCodes.Status404NotFound,
            Instance = AppValue,
            Extensions = { [AppKey] = AppValue }
        }));

        await Assert.That(response.Json[TypeKey]?.GetValue<string>()).IsEqualTo(FrameworkNotFoundType);
        await Assert.That(response.Json[TitleKey]?.GetValue<string>()).IsEqualTo(AppValue);
        await Assert.That(response.Json[StatusKey]?.GetValue<int>()).IsEqualTo(StatusCodes.Status404NotFound);
        await Assert.That(response.Json[InstanceKey]?.GetValue<string>()).IsEqualTo(AppValue);
        await Assert.That(response.Json[AppKey]?.GetValue<string>()).IsEqualTo(AppValue);
        await Assert.That(response.Json.ContainsKey(DetailKey)).IsFalse();
        await Assert.That(response.Json.ContainsKey(InputKey)).IsFalse();
    }

    [Test]
    [MethodDataSource(nameof(UnclaimedTypes))]
    public async Task EmptyProblem_GetsTheResponseStatus(string accept)
    {
        var response = await ThrowAsync(accept, Customize(static c => c.ProblemDetails = new ProblemDetails()));

        await Assert.That(response.Json).HasSingleItem();
        await Assert.That(response.Json[StatusKey]?.GetValue<int>()).IsEqualTo(StatusCodes.Status400BadRequest);
    }

    /// <summary>The values' JSON does not depend on what the client accepts or on the culture.</summary>
    [Test]
    [MethodDataSource(nameof(AcceptedTypes))]
    public async Task ExtensionValues_KeepTheirJsonForm(string accept)
    {
        var id = PersonalId.Parse(OfficialNumbers.Modern.Long);
        var details = new Details(AppValue, HiddenValue);
        var configure = Customize(c =>
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(CommaDecimalCulture);
            c.ProblemDetails.Extensions["double"] = Fraction;
            c.ProblemDetails.Extensions["bool"] = true;
            c.ProblemDetails.Extensions["null"] = null;
            c.ProblemDetails.Extensions["array"] = Numbers;
            c.ProblemDetails.Extensions["object"] = details;
            c.ProblemDetails.Extensions["id"] = id;
        });

        var response = await ThrowAsync(accept, configure);
        var normal = await ThrowAsync(MediaTypeNames.Application.Json, configure);

        await Assert.That(response.WithoutTraceId).IsEqualTo(normal.WithoutTraceId);
        await Assert.That(response.Body).Contains($$"""
            "double":1.5,"bool":true,"null":null,"array":[1,2],"object":{"shown":"{{details.Shown}}"},"id":"{{id.LongFormat()}}"
            """);
        await Assert.That(response.Body).DoesNotContain(details.Hidden);
    }

    [Test]
    [MethodDataSource(nameof(AcceptedTypes))]
    public async Task IdExtension_HasItsConfiguredForm(string accept)
    {
        var id = PersonalId.Parse(OfficialNumbers.Modern.Long);

        var response = await ThrowAsync(accept, services =>
        {
            Customize(c => c.ProblemDetails.Extensions[AppKey] = id)(services);
            services.Configure<CivitasIdSwedenAspNetCoreOptions>(static o => o.JsonFormat = PnrFormat.ShortFormat);
        });

        await Assert.That(response.Json[AppKey]?.GetValue<string>()).IsEqualTo(id.ShortFormat());
    }

    [Test]
    [MethodDataSource(nameof(AcceptedTypes))]
    public async Task DerivedProblem_KeepsItsMembers(string accept)
    {
        var problem = new DerivedProblem { Code = DerivedCode };

        var response = await ThrowAsync(accept, Customize(c => c.ProblemDetails = problem));

        await Assert.That(response.Json["code"]?.GetValue<int>()).IsEqualTo(problem.Code);
    }

    [Test]
    [MethodDataSource(nameof(Unserializable))]
    public async Task Unserializable_SendsNoBody(string accept, Action<IServiceCollection> configure)
    {
        var response = await ThrowAsync(accept, configure);

        await Assert.That(response.Body).IsEmpty();
        await Assert.That(response.Escaped).IsTypeOf<InvalidIdNumberException>();
    }

    [Test]
    [MethodDataSource(nameof(Unserializable))]
    public async Task Unserializable_LeavesTheHandler(string accept, Action<IServiceCollection> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var (failure, length) = await HandleAsync(accept, configure);

        await Assert.That(failure).IsTypeOf<NotSupportedException>();
        await Assert.That(length).IsEqualTo(0);
    }

    /// <summary>
    /// Only a failure that is not thrown reaches the status page, which comes after the exception
    /// handler; the environment sets the default.
    /// </summary>
    [Test]
    [Arguments("Development", null)]
    [Arguments("Production", null)]
    [Arguments("Development", false)]
    [Arguments("Production", true)]
    public async Task MalformedParameter_WithoutJson_HasAStatusPageUnlessThrown(string environment, bool? configured)
    {
        var throws = configured ?? environment == Environments.Development;

        var response = await ProblemProbeHost.SendAsync(
            new ProblemProbeHost.ProbeRequest(ProblemProbeHost.MalformedIdPath)
            {
                Accept = MediaTypeNames.Text.Plain,
                Environment = environment,
                UseExceptionHandler = true
            },
            services =>
            {
                services.AddCivitasIdSwedenAspNetCore();
                if (configured is { } value)
                {
                    services.Configure<RouteHandlerOptions>(o => o.ThrowOnBadRequest = value);
                }
            });

        await Assert.That(response.Status).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(response.Body.Length == 0).IsEqualTo(throws);
    }

    [Test]
    [MethodDataSource(nameof(AcceptedTypes))]
    public async Task Customization_RunsOnceWithTheWholeContext(string accept)
    {
        var contexts = new List<ProblemDetailsContext>();

        await ThrowAsync(accept, Customize(contexts.Add));

        await Assert.That(contexts).HasSingleItem();
        await Assert.That(contexts[0].HttpContext).IsNotNull();
        await Assert.That(contexts[0].Exception).IsTypeOf<InvalidIdNumberException>();
        await Assert.That(contexts[0].ProblemDetails.Type).IsEqualTo(LibraryInvalidIdType);
    }

    [Test]
    [MethodDataSource(nameof(AcceptedTypes))]
    public async Task ClearedCustomization_WritesTheLibraryProblem(string accept)
    {
        var response = await ThrowAsync(accept, static services =>
            services.PostConfigure<ProblemDetailsOptions>(static o => o.CustomizeProblemDetails = null));

        await Assert.That(response.Json[TypeKey]?.GetValue<string>()).IsEqualTo(LibraryInvalidIdType);
        await Assert.That(response.Json.ContainsKey(InputKey)).IsTrue();
    }

    /// <summary>
    /// <c>UseExceptionHandler()</c> logs the app's exception itself and rethrows the original one.
    /// </summary>
    [Test]
    [MethodDataSource(nameof(Failures))]
    public async Task ThrowingCustomization_SendsNoBody(string accept, Func<Exception> failure)
    {
        var response = await ThrowAsync(accept, Customize(_ => throw failure()));

        await Assert.That(response.Body).IsEmpty();
        await Assert.That(response.Escaped).IsTypeOf<InvalidIdNumberException>();
    }

    [Test]
    [MethodDataSource(nameof(Failures))]
    public async Task ThrowingCustomization_LeavesTheHandler(string accept, Func<Exception> failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        var expected = failure();

        var (actual, length) = await HandleAsync(accept, Customize(_ => throw expected));

        await Assert.That(actual).IsSameReferenceAs(expected);
        await Assert.That(length).IsEqualTo(0);
    }

    private static async Task<(Exception? Failure, long Length)> HandleAsync(
        string accept, Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCivitasIdSwedenAspNetCore();
        configure(services);
        await using var provider = services.BuildServiceProvider();
        await using var body = new MemoryStream();
        var context = new DefaultHttpContext { RequestServices = provider, Response = { Body = body } };
        context.Request.Headers.Accept = accept;
        var handler = provider.GetServices<IExceptionHandler>().OfType<InvalidIdNumberExceptionHandler>().Single();

        var failure = await Assert.That(async () => await handler.TryHandleAsync(
                context,
                new InvalidIdNumberException(NotAnId, InvalidIdNumberReason.InvalidFormat),
                CancellationToken.None))
            .Throws<Exception>();

        return (failure, body.Length);
    }

    private static Action<IServiceCollection> Customize(Action<ProblemDetailsContext> customize) =>
        services => services.AddProblemDetails(o => o.CustomizeProblemDetails = customize);

    /// <summary>The metadata a native AOT app has: none for types it did not register.</summary>
    private static Action<IServiceCollection> SourceGeneratedOnly { get; } = static services =>
        services.PostConfigure<HttpJsonOptions>(static o =>
        {
            o.SerializerOptions.TypeInfoResolverChain.Clear();
            o.SerializerOptions.TypeInfoResolverChain.Add(CivitasIdAspNetCoreJsonContext.Default);
        });

    private static Task<ProblemProbeHost.ProbeResponse> ThrowAsync(string accept, Action<IServiceCollection> configure) =>
        ProblemProbeHost.SendAsync(
            new ProblemProbeHost.ProbeRequest(ProblemProbeHost.ThrowPath) { Accept = accept, UseExceptionHandler = true },
            services =>
            {
                services.AddCivitasIdSwedenAspNetCore();
                configure(services);
            });

    private sealed record Details(string Shown, [property: JsonIgnore] string Hidden);

    private sealed class DerivedProblem : ProblemDetails
    {
        public int Code { get; init; }
    }
}
