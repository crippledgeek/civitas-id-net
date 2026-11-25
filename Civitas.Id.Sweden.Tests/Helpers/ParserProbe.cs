using System.Text;
using Civitas.Id.Sweden.Core;
using Microsoft.Extensions.Time.Testing;

namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>Asks every parse and validity entry point that does not throw about a text.</summary>
internal static class ParserProbe
{
    private static readonly DateOnly Today = new(2026, 6, 15);

    private static readonly TimeProvider Clock = new FakeTimeProvider(new DateTimeOffset(Today, TimeOnly.MinValue, TimeSpan.Zero));

    private static readonly IReadOnlyList<EntryPoint> EntryPoints =
    [
        .. Contracts<PersonalId>(),
        new("PersonalId/plain", text => PersonalId.TryParse(text, out _)),
        new("PersonalId/string on a date", text => PersonalId.TryParse(text, Today, out _)),
        new("PersonalId/span on a date", text => PersonalId.TryParse(text.AsSpan(), Today, out _)),
        new("PersonalId/string on a clock", text => PersonalId.TryParse(text, Clock, out _)),
        new("PersonalId/valid", PersonalId.IsValid),
        .. Contracts<CoordinationId>(),
        new("CoordinationId/plain", text => CoordinationId.TryParse(text, out _)),
        new("CoordinationId/string on a date", text => CoordinationId.TryParse(text, Today, out _)),
        new("CoordinationId/span on a date", text => CoordinationId.TryParse(text.AsSpan(), Today, out _)),
        new("CoordinationId/string on a clock", text => CoordinationId.TryParse(text, Clock, out _)),
        new("CoordinationId/valid", CoordinationId.IsValid),
        .. Contracts<OrganisationId>(),
        new("OrganisationId/plain", text => OrganisationId.TryParse(text, out _)),
        new("OrganisationId/string on a date", text => OrganisationId.TryParse(text, Today, out _)),
        new("OrganisationId/span on a date", text => OrganisationId.TryParse(text.AsSpan(), Today, out _)),
        new("OrganisationId/valid", OrganisationId.IsValid),
        new("SwedishOfficialId/plain", text => SwedishOfficialId.TryParseAny(text, out _)),
        new("SwedishOfficialId/string on a date", text => SwedishOfficialId.TryParseAny(text, Today, out _)),
        new("SwedishOfficialId/span on a date", text => SwedishOfficialId.TryParseAny(text.AsSpan(), Today, out _)),
        new("SwedishOfficialId/string on a clock", text => SwedishOfficialId.TryParseAny(text, Clock, out _)),
        new("SwedishOfficialId/valid", SwedishOfficialId.IsValid)
    ];

    /// <summary>How many entry points are asked.</summary>
    public static int Count => EntryPoints.Count;

    /// <summary>The names of the entry points that accept <paramref name="text" />.</summary>
    public static IReadOnlyList<string> AcceptedBy(string text)
    {
        return [.. EntryPoints.Where(entryPoint => entryPoint.Accepts(text)).Select(entryPoint => entryPoint.Name)];
    }

    private static IEnumerable<EntryPoint> Contracts<T>()
        where T : IParsable<T>, ISpanParsable<T>, IUtf8SpanParsable<T>
    {
        yield return new EntryPoint($"{typeof(T).Name}/string", text => T.TryParse(text, null, out _));
        yield return new EntryPoint($"{typeof(T).Name}/span", text => T.TryParse(text.AsSpan(), null, out _));
        yield return new EntryPoint($"{typeof(T).Name}/utf8", text => T.TryParse(Encoding.UTF8.GetBytes(text), null, out _));
    }

    private sealed record EntryPoint(string Name, Func<string, bool> Accepts);
}
