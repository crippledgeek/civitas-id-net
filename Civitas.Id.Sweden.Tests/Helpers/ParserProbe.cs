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

    private const string TryParseName = nameof(PersonalId.TryParse);
    private const string TryParseAnyName = nameof(SwedishOfficialId.TryParseAny);
    private const string IsValidName = nameof(SwedishOfficialId.IsValid);

    private static readonly IReadOnlyList<Probe> Asked =
    [
        .. Contracts<PersonalId>(),
        Try("PersonalId/plain", ParseSignature.Of<PersonalId>(TryParseName, typeof(string)),
            (string text, out PersonalId? result) => PersonalId.TryParse(text, out result)),
        Try("PersonalId/string on a date", ParseSignature.Of<PersonalId>(TryParseName, typeof(string), typeof(DateOnly)),
            (string text, out PersonalId? result) => PersonalId.TryParse(text, Today, out result)),
        Try("PersonalId/span on a date", ParseSignature.Of<PersonalId>(TryParseName, typeof(ReadOnlySpan<char>), typeof(DateOnly)),
            (string text, out PersonalId? result) => PersonalId.TryParse(text.AsSpan(), Today, out result)),
        Try("PersonalId/string on a clock", ParseSignature.Of<PersonalId>(TryParseName, typeof(string), typeof(TimeProvider)),
            (string text, out PersonalId? result) => PersonalId.TryParse(text, Clock, out result)),
        Check("PersonalId/valid", ParseSignature.Of<PersonalId>(IsValidName, typeof(string)), PersonalId.IsValid),
        .. Contracts<CoordinationId>(),
        Try("CoordinationId/plain", ParseSignature.Of<CoordinationId>(TryParseName, typeof(string)),
            (string text, out CoordinationId? result) => CoordinationId.TryParse(text, out result)),
        Try("CoordinationId/string on a date", ParseSignature.Of<CoordinationId>(TryParseName, typeof(string), typeof(DateOnly)),
            (string text, out CoordinationId? result) => CoordinationId.TryParse(text, Today, out result)),
        Try("CoordinationId/span on a date", ParseSignature.Of<CoordinationId>(TryParseName, typeof(ReadOnlySpan<char>), typeof(DateOnly)),
            (string text, out CoordinationId? result) => CoordinationId.TryParse(text.AsSpan(), Today, out result)),
        Try("CoordinationId/string on a clock", ParseSignature.Of<CoordinationId>(TryParseName, typeof(string), typeof(TimeProvider)),
            (string text, out CoordinationId? result) => CoordinationId.TryParse(text, Clock, out result)),
        Check("CoordinationId/valid", ParseSignature.Of<CoordinationId>(IsValidName, typeof(string)), CoordinationId.IsValid),
        .. Contracts<OrganisationId>(),
        Try("OrganisationId/plain", ParseSignature.Of<OrganisationId>(TryParseName, typeof(string)),
            (string text, out OrganisationId? result) => OrganisationId.TryParse(text, out result)),
        Try("OrganisationId/string on a date", ParseSignature.Of<OrganisationId>(TryParseName, typeof(string), typeof(DateOnly)),
            (string text, out OrganisationId? result) => OrganisationId.TryParse(text, Today, out result)),
        Try("OrganisationId/span on a date", ParseSignature.Of<OrganisationId>(TryParseName, typeof(ReadOnlySpan<char>), typeof(DateOnly)),
            (string text, out OrganisationId? result) => OrganisationId.TryParse(text.AsSpan(), Today, out result)),
        Check("OrganisationId/valid", ParseSignature.Of<OrganisationId>(IsValidName, typeof(string)), OrganisationId.IsValid),
        Try("SwedishOfficialId/plain", ParseSignature.Of<SwedishOfficialId>(TryParseAnyName, typeof(string)),
            (string text, out SwedishOfficialId? result) => SwedishOfficialId.TryParseAny(text, out result)),
        Try("SwedishOfficialId/string on a date", ParseSignature.Of<SwedishOfficialId>(TryParseAnyName, typeof(string), typeof(DateOnly)),
            (string text, out SwedishOfficialId? result) => SwedishOfficialId.TryParseAny(text, Today, out result)),
        Try("SwedishOfficialId/span on a date",
            ParseSignature.Of<SwedishOfficialId>(TryParseAnyName, typeof(ReadOnlySpan<char>), typeof(DateOnly)),
            (string text, out SwedishOfficialId? result) => SwedishOfficialId.TryParseAny(text.AsSpan(), Today, out result)),
        Try("SwedishOfficialId/string on a clock",
            ParseSignature.Of<SwedishOfficialId>(TryParseAnyName, typeof(string), typeof(TimeProvider)),
            (string text, out SwedishOfficialId? result) => SwedishOfficialId.TryParseAny(text, Clock, out result)),
        Check("SwedishOfficialId/valid", ParseSignature.Of<SwedishOfficialId>(IsValidName, typeof(string)), SwedishOfficialId.IsValid)
    ];

    /// <summary>The methods the probes ask.</summary>
    public static IReadOnlySet<ParseSignature> Covered { get; } =
        Asked.Select(probe => probe.Covers).ToHashSet();

    /// <summary>How each entry point answers <paramref name="text" />.</summary>
    public static IReadOnlyList<ProbeAnswer> Answers(string text)
    {
        return [.. Asked.Select(probe => probe.Ask(text))];
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

    private static Probe Try<T>(string name, ParseSignature covers, TryRead<T> read)
    {
        return new Probe(covers, text => Ask(name, read, text));
    }

    private static Probe Check(string name, ParseSignature covers, Func<string?, bool> isValid)
    {
        return new Probe(covers, text => new ProbeAnswer(name, isValid(text), LeftResult: null));
    }

    private static IEnumerable<Probe> Contracts<T>()
        where T : IParsable<T>, ISpanParsable<T>, IUtf8SpanParsable<T>
    {
        yield return Try($"{typeof(T).Name}/string", ParseSignature.Of<T>(TryParseName, typeof(string), typeof(IFormatProvider)),
            (string text, out T? result) => T.TryParse(text, null, out result));
        yield return Try($"{typeof(T).Name}/span", ParseSignature.Of<T>(TryParseName, typeof(ReadOnlySpan<char>), typeof(IFormatProvider)),
            (string text, out T? result) => T.TryParse(text.AsSpan(), null, out result));
        yield return Try($"{typeof(T).Name}/utf8", ParseSignature.Of<T>(TryParseName, typeof(ReadOnlySpan<byte>), typeof(IFormatProvider)),
            (string text, out T? result) => T.TryParse(Encoding.UTF8.GetBytes(text), null, out result));
    }

    /// <summary>A read that returns whether it accepts the text, and the result.</summary>
    public delegate bool TryRead<T>(string text, out T? result);

    /// <summary>
    ///     One entry point asked, and the method it declares it asks. Nothing checks that declaration against the
    ///     method the function calls; a probe that names one method and calls another goes unnoticed.
    /// </summary>
    private sealed record Probe(ParseSignature Covers, Func<string, ProbeAnswer> Ask);
}
