using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Errors;

namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>
///     That every entry point of one parsed type rejects a text, and the reason the throwing ones give. The reasons
///     are the current ones: <see cref="SwedishOfficialId.ParseAny(string)" /> reports every rejected text as an
///     unsupported type.
/// </summary>
/// <param name="Parsed">The type whose entry points are asked, as <see cref="Type.Name" /> prefixes the probes' names.</param>
/// <param name="Reason">The reason a throwing entry point gives.</param>
internal sealed record ExpectedRejection(Type Parsed, InvalidIdNumberReason Reason)
{
    /// <summary>A text of the wrong form for <typeparamref name="T" />.</summary>
    public static ExpectedRejection InvalidFormatFor<T>()
        where T : SwedishOfficialId
    {
        return new ExpectedRejection(typeof(T), InvalidIdNumberReason.InvalidFormat);
    }

    /// <summary>A text that none of the types takes, as <see cref="SwedishOfficialId" /> reports it.</summary>
    private static ExpectedRejection UnsupportedByAny { get; } =
        new(typeof(SwedishOfficialId), InvalidIdNumberReason.UnsupportedIdType);

    /// <summary>A text that every type rejects as of the wrong form.</summary>
    public static IReadOnlyList<ExpectedRejection> ByEveryType { get; } = ByEveryTypeAs(InvalidIdNumberReason.InvalidFormat);

    /// <summary>A text that every type rejects, the types of a person or an organisation for <paramref name="reason" />.</summary>
    public static IReadOnlyList<ExpectedRejection> ByEveryTypeAs(InvalidIdNumberReason reason)
    {
        return
        [
            new ExpectedRejection(typeof(PersonalId), reason), new ExpectedRejection(typeof(CoordinationId), reason),
            new ExpectedRejection(typeof(OrganisationId), reason), UnsupportedByAny
        ];
    }

    /// <summary>
    ///     The entry points, by name and how they answer, that do not reject <paramref name="text" /> as expected: those
    ///     that do not throw accept it or leave a result, and those that throw give another reason. Empty when every one
    ///     rejects it. A name never holds the text.
    /// </summary>
    /// <exception cref="InvalidOperationException">A probe asks no entry point of an expected type.</exception>
    public static IReadOnlyList<string> NotRejected(string text, IEnumerable<ExpectedRejection> expected)
    {
        return NotRejected(ProbedText.Of(text), expected);
    }

    /// <summary>The entry points, among those <paramref name="probed" /> holds, that do not reject as expected.</summary>
    public static IReadOnlyList<string> NotRejected(ProbedText probed, IEnumerable<ExpectedRejection> expected)
    {
        return [.. expected.SelectMany(rejection => rejection.NotRejected(probed.For(rejection.Parsed)))];
    }

    /// <inheritdoc cref="NotRejected(string, IEnumerable{ExpectedRejection})" />
    public static IReadOnlyList<string> NotRejected(string text, params ExpectedRejection[] expected)
    {
        return NotRejected(text, expected.AsEnumerable());
    }

    /// <summary>The entry points among <paramref name="results" /> that do not reject as this expects.</summary>
    public IReadOnlyList<string> NotRejected(TypeResults results)
    {
        return
        [
            .. results.Tries.Where(answer => answer.Accepted).Select(answer => $"{answer.Name} accepts"),
            .. results.Tries.Where(answer => answer is { Accepted: false, LeftResult: true })
                .Select(answer => $"{answer.Name} rejects and leaves a result"),
            .. results.Throws.Where(answer => answer.Reason != Reason)
                .Select(answer => $"{answer.Name} throws {answer.Reason?.ToString() ?? "nothing"}, expected {Reason}")
        ];
    }
}
