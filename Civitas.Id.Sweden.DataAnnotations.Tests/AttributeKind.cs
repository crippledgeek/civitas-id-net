using System.ComponentModel.DataAnnotations;
using Civitas.Id.Sweden.Core;

namespace Civitas.Id.Sweden.DataAnnotations.Tests;

/// <summary>
///     One of the four attributes with what its tests need: a number it accepts, an instance it accepts and its
///     default message. The numbers come from the pinned sets.
/// </summary>
/// <param name="Create">Makes a new attribute, since a test may set its message.</param>
/// <param name="Accepted">A number the attribute accepts, as text.</param>
/// <param name="AcceptedInstance">An instance the attribute accepts.</param>
/// <param name="DefaultTemplate">The default message, with <c>{0}</c> for the name.</param>
internal sealed record AttributeKind(
    Func<ValidationAttribute> Create,
    string Accepted,
    object AcceptedInstance,
    string DefaultTemplate)
{
    /// <summary>The personnummer attribute.</summary>
    public static readonly AttributeKind Personal = new(
        () => new ValidPersonalIdAttribute(),
        OfficialNumbers.Centenarian.Long,
        PersonalId.Parse(OfficialNumbers.Centenarian.Long),
        "The {0} field is not a valid Swedish personnummer.");

    /// <summary>The samordningsnummer attribute.</summary>
    public static readonly AttributeKind Coordination = new(
        () => new ValidCoordinationIdAttribute(),
        OfficialNumbers.CentenarianSamordningsnummer.Long,
        CoordinationId.Parse(OfficialNumbers.CentenarianSamordningsnummer.Long),
        "The {0} field is not a valid Swedish samordningsnummer.");

    /// <summary>The organisationsnummer attribute.</summary>
    public static readonly AttributeKind Organisation = new(
        () => new ValidOrganisationIdAttribute(),
        LegalPersonNumbers.Aktiebolag.Digits,
        OrganisationId.Parse(LegalPersonNumbers.Aktiebolag.Digits),
        "The {0} field is not a valid Swedish organisationsnummer.");

    /// <summary>The attribute that accepts any of the three.</summary>
    public static readonly AttributeKind Official = new(
        () => new ValidSwedishOfficialIdAttribute(),
        OfficialNumbers.Centenarian.Long,
        PersonalId.Parse(OfficialNumbers.Centenarian.Long),
        "The {0} field is not a valid Swedish official ID.");

    /// <summary>The default message for a field called <paramref name="name" />.</summary>
    public string DefaultText(string name)
    {
        return DefaultTemplate.Replace("{0}", name, StringComparison.Ordinal);
    }
}
