using System.Text;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Errors;

namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>Asks every parse entry point that throws about a text.</summary>
internal static class ThrowingParserProbe
{
    private const string ParseName = nameof(PersonalId.Parse);
    private const string ParseAnyName = nameof(SwedishOfficialId.ParseAny);

    private static readonly IReadOnlyList<EntryPoint> Asked =
    [
        .. Contracts<PersonalId>(),
        new("PersonalId/plain", ParseSignature.Of<PersonalId>(ParseName, typeof(string)), PersonalId.Parse),
        new("PersonalId/string on a date", ParseSignature.Of<PersonalId>(ParseName, typeof(string), typeof(DateOnly)),
            text => PersonalId.Parse(text, ParserProbe.Today)),
        new("PersonalId/span on a date", ParseSignature.Of<PersonalId>(ParseName, typeof(ReadOnlySpan<char>), typeof(DateOnly)),
            text => PersonalId.Parse(text.AsSpan(), ParserProbe.Today)),
        new("PersonalId/string on a clock", ParseSignature.Of<PersonalId>(ParseName, typeof(string), typeof(TimeProvider)),
            text => PersonalId.Parse(text, ParserProbe.Clock)),
        .. Contracts<CoordinationId>(),
        new("CoordinationId/plain", ParseSignature.Of<CoordinationId>(ParseName, typeof(string)), CoordinationId.Parse),
        new("CoordinationId/string on a date", ParseSignature.Of<CoordinationId>(ParseName, typeof(string), typeof(DateOnly)),
            text => CoordinationId.Parse(text, ParserProbe.Today)),
        new("CoordinationId/span on a date", ParseSignature.Of<CoordinationId>(ParseName, typeof(ReadOnlySpan<char>), typeof(DateOnly)),
            text => CoordinationId.Parse(text.AsSpan(), ParserProbe.Today)),
        new("CoordinationId/string on a clock", ParseSignature.Of<CoordinationId>(ParseName, typeof(string), typeof(TimeProvider)),
            text => CoordinationId.Parse(text, ParserProbe.Clock)),
        .. Contracts<OrganisationId>(),
        new("OrganisationId/plain", ParseSignature.Of<OrganisationId>(ParseName, typeof(string)), OrganisationId.Parse),
        new("OrganisationId/string on a date", ParseSignature.Of<OrganisationId>(ParseName, typeof(string), typeof(DateOnly)),
            text => OrganisationId.Parse(text, ParserProbe.Today)),
        new("OrganisationId/span on a date", ParseSignature.Of<OrganisationId>(ParseName, typeof(ReadOnlySpan<char>), typeof(DateOnly)),
            text => OrganisationId.Parse(text.AsSpan(), ParserProbe.Today)),
        new("SwedishOfficialId/plain", ParseSignature.Of<SwedishOfficialId>(ParseAnyName, typeof(string)), SwedishOfficialId.ParseAny),
        new("SwedishOfficialId/string on a date", ParseSignature.Of<SwedishOfficialId>(ParseAnyName, typeof(string), typeof(DateOnly)),
            text => SwedishOfficialId.ParseAny(text, ParserProbe.Today)),
        new("SwedishOfficialId/span on a date",
            ParseSignature.Of<SwedishOfficialId>(ParseAnyName, typeof(ReadOnlySpan<char>), typeof(DateOnly)),
            text => SwedishOfficialId.ParseAny(text.AsSpan(), ParserProbe.Today)),
        new("SwedishOfficialId/string on a clock",
            ParseSignature.Of<SwedishOfficialId>(ParseAnyName, typeof(string), typeof(TimeProvider)),
            text => SwedishOfficialId.ParseAny(text, ParserProbe.Clock))
    ];

    /// <summary>How many entry points are asked.</summary>
    public static int Count => Asked.Count;

    /// <summary>The methods the probe asks.</summary>
    public static IReadOnlySet<ParseSignature> Covered { get; } =
        Asked.Select(asked => asked.Covers).ToHashSet();

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
        yield return new EntryPoint($"{typeof(T).Name}/string",
            ParseSignature.Of<T>(ParseName, typeof(string), typeof(IFormatProvider)), text => T.Parse(text, null));
        yield return new EntryPoint($"{typeof(T).Name}/span",
            ParseSignature.Of<T>(ParseName, typeof(ReadOnlySpan<char>), typeof(IFormatProvider)), text => T.Parse(text.AsSpan(), null));
        yield return new EntryPoint($"{typeof(T).Name}/utf8",
            ParseSignature.Of<T>(ParseName, typeof(ReadOnlySpan<byte>), typeof(IFormatProvider)), text => T.Parse(Encoding.UTF8.GetBytes(text), null));
    }

    /// <summary>
    ///     One entry point asked, and the method it declares it asks. Nothing checks that declaration against the
    ///     method the function calls; an entry point that names one method and calls another goes unnoticed.
    /// </summary>
    private sealed record EntryPoint(string Name, ParseSignature Covers, Func<string, SwedishOfficialId> Parse);
}
