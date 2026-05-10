namespace Civitas.Id.Sweden.Format;

/// <summary>
///     Distinguishes whether an organisation number represents a legal person
///     (juridisk person) or a physical person (fysisk person, sole proprietor).
/// </summary>
public enum OrganisationNumberType
{
    /// <summary>Legal person — companies, foundations, associations, etc.</summary>
    LegalPerson,

    /// <summary>Physical person — sole proprietor using their personnummer/samordningsnummer.</summary>
    PhysicalPerson
}
