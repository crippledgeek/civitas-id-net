using Civitas.Id.Sweden.Core;
using JetBrains.Annotations;

namespace Civitas.Id.Sweden.Fakers;

/// <summary>
///     Faker contract for Swedish organisationsnummer.
/// </summary>
/// <remarks>
///     Non-generic because <see cref="OrganisationId" /> is a sealed concrete type
///     (no subtypes); a type parameter would be a single-valued degenerate.
/// </remarks>
[PublicAPI]
public interface IOrganisationIdFaker : IIdFaker<OrganisationId>
{
    /// <summary>Generates a valid legal-person organisationsnummer (random Bolagsverket form).</summary>
    OrganisationId GenerateLegalPerson();

    /// <summary>
    ///     Generates a valid Enskild firma organisationsnummer (physical-person variant).
    /// </summary>
    OrganisationId GeneratePhysicalPerson();
}
