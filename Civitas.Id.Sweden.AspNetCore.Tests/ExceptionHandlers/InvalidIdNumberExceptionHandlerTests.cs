using System.Text.Json;
using System.Text.Json.Nodes;
using Civitas.Id.Sweden.AspNetCore.ExceptionHandlers;
using Civitas.Id.Sweden.AspNetCore.Options;
using Civitas.Id.Sweden.Errors;
using Microsoft.Extensions.Logging.Abstractions;

namespace Civitas.Id.Sweden.AspNetCore.Tests.ExceptionHandlers;

public class InvalidIdNumberExceptionHandlerTests
{
    /// <summary>
    /// <see cref="IProblemDetailsService"/> stub that always returns
    /// <see langword="false"/> so the handler engages its hand-written
    /// AOT-safe fallback path — which is what these unit tests assert.
    /// Integration tests exercise the <see cref="IProblemDetailsService"/>
    /// success path against a real service-provider build.
    /// </summary>
    private sealed class FallbackOnlyProblemDetailsService : IProblemDetailsService
    {
        public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context) => new(false);

        public ValueTask WriteAsync(ProblemDetailsContext context) => ValueTask.CompletedTask;
    }

    private static InvalidIdNumberExceptionHandler Make(Func<string, string>? redact = null)
    {
        var opts = Microsoft.Extensions.Options.Options.Create(
            new CivitasIdSwedenAspNetCoreOptions { RedactInput = redact });
        return new InvalidIdNumberExceptionHandler(
            opts,
            new FallbackOnlyProblemDetailsService(),
            NullLogger<InvalidIdNumberExceptionHandler>.Instance);
    }

    private static DefaultHttpContext MakeContext()
    {
        return new DefaultHttpContext { Response = { Body = new MemoryStream() } };
    }

    public class TryHandleAsync
    {
        [Test]
        public async Task ReturnsTrue_ForInvalidIdNumberException()
        {
            var handler = Make();
            var ctx = MakeContext();
            var ex = new InvalidIdNumberException("19811218bad9", InvalidIdNumberReason.InvalidFormat);

            var handled = await handler.TryHandleAsync(ctx, ex, CancellationToken.None);

            await Assert.That(handled).IsTrue();
            await Assert.That(ctx.Response.StatusCode).IsEqualTo(StatusCodes.Status400BadRequest);
        }

        [Test]
        public async Task ReturnsFalse_ForOtherException()
        {
            var handler = Make();
            var ctx = MakeContext();
            var ex = new InvalidOperationException("not ours");

            var handled = await handler.TryHandleAsync(ctx, ex, CancellationToken.None);

            await Assert.That(handled).IsFalse();
        }

        [Test]
        public async Task UsesCustomRedaction_WhenProvided()
        {
            var handler = Make(_ => "REDACTED");
            var ctx = MakeContext();
            var ex = new InvalidIdNumberException("198112189876", InvalidIdNumberReason.InvalidChecksum);

            await handler.TryHandleAsync(ctx, ex, CancellationToken.None);

            ctx.Response.Body.Position = 0;
            var body = await JsonSerializer.DeserializeAsync<JsonObject>(ctx.Response.Body);
            await Assert.That(body!["input"]?.ToString()).IsEqualTo("REDACTED");
        }

        [Test]
        public async Task FallsBackTo_ExceptionRedactedInput_WhenNoCustomRedact()
        {
            var handler = Make();
            var ctx = MakeContext();
            var ex = new InvalidIdNumberException("198112189876", InvalidIdNumberReason.InvalidChecksum);

            await handler.TryHandleAsync(ctx, ex, CancellationToken.None);

            ctx.Response.Body.Position = 0;
            var body = await JsonSerializer.DeserializeAsync<JsonObject>(ctx.Response.Body);
            await Assert.That(body!["input"]?.ToString()).IsEqualTo(ex.RedactedInput);
        }

        [Test]
        public async Task HandWrittenFallback_IncludesTraceId()
        {
            var handler = Make();
            var ctx = MakeContext();
            ctx.TraceIdentifier = "test-trace-1234";
            var ex = new InvalidIdNumberException("198112189876", InvalidIdNumberReason.InvalidChecksum);

            await handler.TryHandleAsync(ctx, ex, CancellationToken.None);

            ctx.Response.Body.Position = 0;
            using var reader = new StreamReader(ctx.Response.Body);
            var body = await reader.ReadToEndAsync();
            await Assert.That(body).Contains("\"traceId\":");
        }

        [Test]
        public async Task IncludesReasonInExtensions()
        {
            var handler = Make();
            var ctx = MakeContext();
            var ex = new InvalidIdNumberException("19811218bad9", InvalidIdNumberReason.InvalidChecksum);

            await handler.TryHandleAsync(ctx, ex, CancellationToken.None);

            ctx.Response.Body.Position = 0;
            var body = await JsonSerializer.DeserializeAsync<JsonObject>(ctx.Response.Body);
            await Assert.That(body!["reason"]?.ToString()).IsEqualTo("InvalidChecksum");
        }

        [Test]
        public async Task NullHttpContext_Throws()
        {
            var handler = Make();
            var ex = new InvalidIdNumberException("198112189876", InvalidIdNumberReason.InvalidChecksum);

            await Assert.That(async () =>
                    await handler.TryHandleAsync(null!, ex, CancellationToken.None))
                .Throws<ArgumentNullException>();
        }

        [Test]
        public async Task NullException_Throws()
        {
            var handler = Make();
            var ctx = MakeContext();

            await Assert.That(async () =>
                    await handler.TryHandleAsync(ctx, null!, CancellationToken.None))
                .Throws<ArgumentNullException>();
        }
    }
}
