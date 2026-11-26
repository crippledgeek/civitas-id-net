using System.Text;
using Civitas.Id.Sweden.Core;
using Microsoft.Extensions.Time.Testing;

namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>
///     Asks every parse and validity entry point that does not throw about a text. The entry points without a date or a
///     clock read the Stockholm clock until it is read only where a ten-digit number needs it, so they are asked only
///     about texts whose outcome does not depend on the date.
/// </summary>
internal static class ParserProbe
{
    /// <summary>The date that the probes read ten-digit numbers on.</summary>
    public static readonly DateOnly Today = new(2026, 6, 15);

    /// <summary>A clock on <see cref="Today" />.</summary>
    public static readonly TimeProvider Clock = new FakeTimeProvider(new DateTimeOffset(Today, TimeOnly.MinValue, TimeSpan.Zero));

    private static readonly IReadOnlyList<Func<string, ProbeAnswer>> Asked =
    [
        .. Contracts<PersonalId>(),
        Try("PersonalId/plain", (string text, out PersonalId? result) => PersonalId.TryParse(text, out result)),
        Try("PersonalId/string on a date", (string text, out PersonalId? result) => PersonalId.TryParse(text, Today, out result)),
        Try("PersonalId/span on a date", (string text, out PersonalId? result) => PersonalId.TryParse(text.AsSpan(), Today, out result)),
        Try("PersonalId/string on a clock", (string text, out PersonalId? result) => PersonalId.TryParse(text, Clock, out result)),
        Check("PersonalId/valid", PersonalId.IsValid),
        .. Contracts<CoordinationId>(),
        Try("CoordinationId/plain", (string text, out CoordinationId? result) => CoordinationId.TryParse(text, out result)),
        Try("CoordinationId/string on a date", (string text, out CoordinationId? result) => CoordinationId.TryParse(text, Today, out result)),
        Try("CoordinationId/span on a date", (string text, out CoordinationId? result) => CoordinationId.TryParse(text.AsSpan(), Today, out result)),
        Try("CoordinationId/string on a clock", (string text, out CoordinationId? result) => CoordinationId.TryParse(text, Clock, out result)),
        Check("CoordinationId/valid", CoordinationId.IsValid),
        .. Contracts<OrganisationId>(),
        Try("OrganisationId/plain", (string text, out OrganisationId? result) => OrganisationId.TryParse(text, out result)),
        Try("OrganisationId/string on a date", (string text, out OrganisationId? result) => OrganisationId.TryParse(text, Today, out result)),
        Try("OrganisationId/span on a date", (string text, out OrganisationId? result) => OrganisationId.TryParse(text.AsSpan(), Today, out result)),
        Check("OrganisationId/valid", OrganisationId.IsValid),
        Try("SwedishOfficialId/plain", (string text, out SwedishOfficialId? result) => SwedishOfficialId.TryParseAny(text, out result)),
        Try("SwedishOfficialId/string on a date", (string text, out SwedishOfficialId? result) => SwedishOfficialId.TryParseAny(text, Today, out result)),
        Try("SwedishOfficialId/span on a date", (string text, out SwedishOfficialId? result) => SwedishOfficialId.TryParseAny(text.AsSpan(), Today, out result)),
        Try("SwedishOfficialId/string on a clock", (string text, out SwedishOfficialId? result) => SwedishOfficialId.TryParseAny(text, Clock, out result)),
        Check("SwedishOfficialId/valid", SwedishOfficialId.IsValid)
    ];

    /// <summary>How many entry points are asked.</summary>
    public static int Count => Asked.Count;

    /// <summary>How each entry point answers <paramref name="text" />.</summary>
    public static IReadOnlyList<ProbeAnswer> Answers(string text)
    {
        return [.. Asked.Select(ask => ask(text))];
    }

    /// <summary>The names of the entry points that accept <paramref name="text" />.</summary>
    public static IReadOnlyList<string> AcceptedBy(string text)
    {
        return [.. Answers(text).Where(answer => answer.Accepted).Select(answer => answer.Name)];
    }

    /// <summary>Asks one entry point that returns its result in an out parameter.</summary>
    public static ProbeAnswer Ask<T>(string name, TryRead<T> read, string text)
    {
        var accepted = read(text, out var result);
        return new ProbeAnswer(name, accepted, result is not null);
    }

    private static Func<string, ProbeAnswer> Try<T>(string name, TryRead<T> read)
    {
        return text => Ask(name, read, text);
    }

    private static Func<string, ProbeAnswer> Check(string name, Func<string?, bool> isValid)
    {
        return text => new ProbeAnswer(name, isValid(text), LeftResult: null);
    }

    private static IEnumerable<Func<string, ProbeAnswer>> Contracts<T>()
        where T : IParsable<T>, ISpanParsable<T>, IUtf8SpanParsable<T>
    {
        yield return Try($"{typeof(T).Name}/string", (string text, out T? result) => T.TryParse(text, null, out result));
        yield return Try($"{typeof(T).Name}/span", (string text, out T? result) => T.TryParse(text.AsSpan(), null, out result));
        yield return Try($"{typeof(T).Name}/utf8", (string text, out T? result) => T.TryParse(Encoding.UTF8.GetBytes(text), null, out result));
    }

    /// <summary>A read that returns whether it accepts the text, and the result.</summary>
    public delegate bool TryRead<T>(string text, out T? result);
}
