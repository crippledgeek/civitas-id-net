namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>
///     A static parse or validity method as text: its type, its name and the types of the parameters a caller
///     passes in. The result parameter of a try method is left out, because it is always of the declaring type.
/// </summary>
internal sealed record ParseSignature
{
    private ParseSignature(string text)
    {
        Text = text;
    }

    /// <summary>The signature as text.</summary>
    private string Text { get; }

    /// <summary>The signature of <paramref name="method" /> on <paramref name="declaring" />.</summary>
    public static ParseSignature Of(Type declaring, string method, IEnumerable<Type> inputs)
    {
        ArgumentNullException.ThrowIfNull(declaring);
        return new ParseSignature($"{declaring.Name}.{method}({string.Join(", ", inputs.Select(NameOf))})");
    }

    /// <summary>The signature of <paramref name="method" /> on <typeparamref name="T" />.</summary>
    public static ParseSignature Of<T>(string method, params Type[] inputs)
    {
        return Of(typeof(T), method, inputs);
    }

    /// <summary>
    ///     Each method that no probe asks, and each probe that asks no method: an empty list when they agree.
    /// </summary>
    public static IEnumerable<string> Differences(IReadOnlySet<ParseSignature> methods, IReadOnlySet<ParseSignature> probed)
    {
        return methods.Except(probed).Select(missing => $"not probed: {missing}")
            .Concat(probed.Except(methods).Select(extra => $"probed but not a parse method: {extra}"));
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Text;
    }

    private static string NameOf(Type type)
    {
        return type.IsGenericType
            ? $"{type.Name[..type.Name.IndexOf('`', StringComparison.Ordinal)]}<{string.Join(", ", type.GetGenericArguments().Select(NameOf))}>"
            : type.Name;
    }
}
