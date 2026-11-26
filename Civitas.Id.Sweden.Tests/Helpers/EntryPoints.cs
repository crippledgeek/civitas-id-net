using System.Reflection;
using Civitas.Id.Sweden.Core;

namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>
///     Reads the public static entry points from the types, so that a probe cannot miss a new one. Reflection, because
///     only enumerating the members shows that none is missing.
/// </summary>
internal static class EntryPoints
{
    private static readonly IReadOnlyList<Type> ParsedTypes =
        [typeof(PersonalId), typeof(CoordinationId), typeof(OrganisationId), typeof(SwedishOfficialId)];

    private static readonly IReadOnlyList<string> ParsableNames =
        [nameof(IParsable<>.Parse), nameof(IParsable<>.TryParse)];

    /// <summary>
    ///     The public static methods of the parsed types with one of <paramref name="names" />, and the string form of
    ///     <see cref="IParsable{TSelf}" /> under each of those names for every type that implements it. That form is
    ///     implemented explicitly, so it is not among the public methods.
    /// </summary>
    public static IReadOnlySet<ParseSignature> Named(params string[] names)
    {
        const BindingFlags declaredStatic = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;
        var methods = ParsedTypes.SelectMany(type => type.GetMethods(declaredStatic)
            .Where(method => names.Contains(method.Name))
            .Select(method => ParseSignature.Of(type, method.Name,
                method.GetParameters().Where(parameter => !parameter.IsOut).Select(parameter => parameter.ParameterType))));
        var stringForms = ParsedTypes.Where(ImplementsParsable)
            .SelectMany(type => names.Intersect(ParsableNames)
                .Select(name => ParseSignature.Of(type, name, [typeof(string), typeof(IFormatProvider)])));
        return methods.Concat(stringForms).ToHashSet();
    }

    private static bool ImplementsParsable(Type type)
    {
        return type.GetInterfaces().Any(contract =>
            contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IParsable<>));
    }
}
