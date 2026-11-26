namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>
///     That every entry point of the parsed types takes a text: the ones that return whether they do say so and leave a
///     result, and the ones that throw do not. The counterpart of <see cref="ExpectedRejection" />.
/// </summary>
internal static class ExpectedAcceptance
{
    /// <summary>
    ///     The entry points, by name and how they answer, that do not accept <paramref name="text" />. Empty when every
    ///     one of the types takes it. A name never holds the text.
    /// </summary>
    /// <exception cref="InvalidOperationException">A probe asks no entry point of a type.</exception>
    public static IReadOnlyList<string> NotAccepted(string text, params Type[] parsed)
    {
        return NotAccepted(ProbedText.Of(text), parsed);
    }

    /// <summary>The entry points of the types, among those <paramref name="probed" /> holds, that do not accept.</summary>
    public static IReadOnlyList<string> NotAccepted(ProbedText probed, params Type[] parsed)
    {
        return [.. parsed.SelectMany(type => NotAccepted(probed.For(type)))];
    }

    /// <summary>The entry points among <paramref name="results" /> that do not accept.</summary>
    public static IReadOnlyList<string> NotAccepted(TypeResults results)
    {
        return
        [
            .. results.Tries.Where(answer => !answer.Accepted).Select(answer => $"{answer.Name} rejects"),
            .. results.Tries.Where(answer => answer is { Accepted: true, LeftResult: false })
                .Select(answer => $"{answer.Name} accepts and leaves no result"),
            .. results.Throws.Where(answer => answer.Reason is not null)
                .Select(answer => $"{answer.Name} throws {answer.Reason}")
        ];
    }
}
