using System.Reflection;
using Civitas.Id.Sweden.Core;

namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>Counts entry points from the types, so that a probe cannot forget a new one.</summary>
internal static class EntryPoints
{
    private static readonly IReadOnlyList<Type> ParsedTypes =
        [typeof(PersonalId), typeof(CoordinationId), typeof(OrganisationId), typeof(SwedishOfficialId)];

    /// <summary>
    ///     How many types implement the string form of <see cref="IParsable{TSelf}" />. They do so explicitly, so it is
    ///     not among the public methods.
    /// </summary>
    public static int ExplicitCount => ParsedTypes.Count(type => type.GetInterfaces().Any(contract =>
        contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IParsable<>)));

    /// <summary>The public methods with the given names, and the explicit ones.</summary>
    public static int CountOf(params string[] names)
    {
        const BindingFlags declaredStatic = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;
        return ExplicitCount + ParsedTypes.SelectMany(type => type.GetMethods(declaredStatic))
            .Count(method => names.Contains(method.Name));
    }
}
