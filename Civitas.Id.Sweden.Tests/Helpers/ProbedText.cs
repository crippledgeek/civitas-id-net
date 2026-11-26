using Civitas.Id.Sweden.Errors;

namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>How every entry point of both probes answers one text, asked once however many types are then looked at.</summary>
internal sealed class ProbedText
{
    private readonly IReadOnlyList<ProbeAnswer> _tries;
    private readonly IReadOnlyList<(string Name, InvalidIdNumberReason? Reason)> _throws;

    private ProbedText(IReadOnlyList<ProbeAnswer> tries, IReadOnlyList<(string Name, InvalidIdNumberReason? Reason)> throws)
    {
        _tries = tries;
        _throws = throws;
    }

    /// <summary>Asks both probes about <paramref name="text" />, once each.</summary>
    public static ProbedText Of(string text)
    {
        return Of(text, ParserProbe.Answers, ThrowingParserProbe.ReasonsFor);
    }

    /// <summary>Asks the two given probes about <paramref name="text" />, once each.</summary>
    public static ProbedText Of(
        string text,
        Func<string, IReadOnlyList<ProbeAnswer>> tries,
        Func<string, IReadOnlyList<(string Name, InvalidIdNumberReason? Reason)>> throws)
    {
        return new ProbedText(tries(text), throws(text));
    }

    /// <summary>The answers of the entry points of <paramref name="parsed" />.</summary>
    /// <exception cref="InvalidOperationException">A probe asks no entry point of the type.</exception>
    public TypeResults For(Type parsed)
    {
        var prefix = parsed.Name + "/";
        var results = new TypeResults(
            [.. _tries.Where(answer => answer.Name.StartsWith(prefix, StringComparison.Ordinal))],
            [.. _throws.Where(answer => answer.Name.StartsWith(prefix, StringComparison.Ordinal))]);
        if (results.Tries.Count == 0 || results.Throws.Count == 0)
            throw new InvalidOperationException($"A probe asks no entry point of {parsed.Name}.");
        return results;
    }
}
