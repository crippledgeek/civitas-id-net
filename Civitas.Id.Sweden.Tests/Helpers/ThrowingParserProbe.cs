using System.Text;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Errors;

namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>Asks every parse entry point that throws about a text.</summary>
internal static class ThrowingParserProbe
{
    private static readonly IReadOnlyList<EntryPoint> Asked =
    [
        .. Contracts<PersonalId>(),
        new("PersonalId/plain", PersonalId.Parse),
        new("PersonalId/string on a date", text => PersonalId.Parse(text, ParserProbe.Today)),
        new("PersonalId/span on a date", text => PersonalId.Parse(text.AsSpan(), ParserProbe.Today)),
        new("PersonalId/string on a clock", text => PersonalId.Parse(text, ParserProbe.Clock)),
        .. Contracts<CoordinationId>(),
        new("CoordinationId/plain", CoordinationId.Parse),
        new("CoordinationId/string on a date", text => CoordinationId.Parse(text, ParserProbe.Today)),
        new("CoordinationId/span on a date", text => CoordinationId.Parse(text.AsSpan(), ParserProbe.Today)),
        new("CoordinationId/string on a clock", text => CoordinationId.Parse(text, ParserProbe.Clock)),
        .. Contracts<OrganisationId>(),
        new("OrganisationId/plain", OrganisationId.Parse),
        new("OrganisationId/string on a date", text => OrganisationId.Parse(text, ParserProbe.Today)),
        new("OrganisationId/span on a date", text => OrganisationId.Parse(text.AsSpan(), ParserProbe.Today)),
        new("SwedishOfficialId/plain", SwedishOfficialId.ParseAny),
        new("SwedishOfficialId/string on a date", text => SwedishOfficialId.ParseAny(text, ParserProbe.Today)),
        new("SwedishOfficialId/span on a date", text => SwedishOfficialId.ParseAny(text.AsSpan(), ParserProbe.Today)),
        new("SwedishOfficialId/string on a clock", text => SwedishOfficialId.ParseAny(text, ParserProbe.Clock))
    ];

    /// <summary>How many entry points are asked.</summary>
    public static int Count => Asked.Count;

    /// <summary>
    ///     Why each entry point rejects <paramref name="text" />, or <see langword="null" /> where it is accepted.
    /// </summary>
    public static IReadOnlyList<(string Name, InvalidIdNumberReason? Reason)> ReasonsFor(string text)
    {
        return [.. Asked.Select(asked => (asked.Name, Rejections.ReasonOf(() => asked.Parse(text))))];
    }

    private static IEnumerable<EntryPoint> Contracts<T>()
        where T : SwedishOfficialId, IParsable<T>, ISpanParsable<T>, IUtf8SpanParsable<T>
    {
        yield return new EntryPoint($"{typeof(T).Name}/string", text => T.Parse(text, null));
        yield return new EntryPoint($"{typeof(T).Name}/span", text => T.Parse(text.AsSpan(), null));
        yield return new EntryPoint($"{typeof(T).Name}/utf8", text => T.Parse(Encoding.UTF8.GetBytes(text), null));
    }

    private sealed record EntryPoint(string Name, Func<string, SwedishOfficialId> Parse);
}
