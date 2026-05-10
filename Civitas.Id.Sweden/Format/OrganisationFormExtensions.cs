using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace Civitas.Id.Sweden.Format;

/// <summary>
///     Extension members and lookup helpers for <see cref="OrganisationForm" />.
/// </summary>
[SuppressMessage(
    "Design", "CA1034:Nested types should not be visible",
    Justification = "C# 14 'extension(T)' block is compiled to a nested type by the language; not authored by us.")]
public static class OrganisationFormExtensions
{
    /// <summary>
    ///     Looks up an <see cref="OrganisationForm" /> by its numeric code.
    ///     Returns null when the code is not a recognised form.
    /// </summary>
    /// <param name="code">The two-digit form code.</param>
    /// <returns>The matching <see cref="OrganisationForm" />, or null if unknown.</returns>
    [Pure]
    public static OrganisationForm? FromCode(int code)
    {
        return Enum.IsDefined((OrganisationForm)code) ? (OrganisationForm)code : null;
    }

    /// <summary>
    ///     Extracts the organisation form encoded in an organisation number string.
    ///     Accepts 10-digit, 12-digit (16NNNNNNNNNN), and separated forms.
    ///     Returns <see cref="OrganisationForm.JuridiskFormEjUtredd" /> when the code is unknown.
    /// </summary>
    /// <param name="organisationNumber">The organisation number to parse.</param>
    /// <returns>
    ///     The encoded <see cref="OrganisationForm" />, or <see cref="OrganisationForm.JuridiskFormEjUtredd" /> on
    ///     unknown.
    /// </returns>
    [Pure]
    public static OrganisationForm FromOrganisationNumber(string organisationNumber)
    {
        ArgumentNullException.ThrowIfNull(organisationNumber);

        var cleaned = organisationNumber
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("+", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal);

        var startPos = cleaned switch
        {
            { Length: 10 } => 0,
            { Length: 12 } when cleaned.StartsWith("16", StringComparison.Ordinal) => 2,
            _ => -1
        };

        if (startPos < 0 || cleaned.Length < startPos + 2
            || !int.TryParse(cleaned.AsSpan(startPos, 2), out var formCode))
            return OrganisationForm.JuridiskFormEjUtredd;

        return FromCode(formCode) ?? OrganisationForm.JuridiskFormEjUtredd;
    }

    extension(OrganisationForm form)
    {
        /// <summary>The two-digit form code (positions 1-2 of the organisation number).</summary>
        public int Code => (int)form;

        /// <summary>Swedish-language description of the form.</summary>
        public string Description => form switch
        {
            OrganisationForm.None => "Ingen organisationsform - Fysisk person",
            OrganisationForm.EnklaBolag => "Enkla bolag",
            OrganisationForm.Partrederier => "Partrederier",
            OrganisationForm.HandelsbolagKommanditbolag => "Handelsbolag, kommanditbolag",
            OrganisationForm.Gruvbolag => "Gruvbolag",
            OrganisationForm.Bankaktiebolag => "Bankaktiebolag",
            OrganisationForm.Forsakringsaktiebolag => "Försäkringsaktiebolag",
            OrganisationForm.Europabolag => "Europabolag",
            OrganisationForm.AktiebolagOvriga => "Övriga aktiebolag",
            OrganisationForm.EkonomiskaForeningar => "Ekonomiska föreningar",
            OrganisationForm.Bostadsrattsforeningar => "Bostadsrättsföreningar",
            OrganisationForm.KooperativHyresrattsforening => "Kooperativ Hyresrättsförening",
            OrganisationForm.EuropakooperativEgtsEric => "Europakooperativ, EGTS och Eric-konsortier",
            OrganisationForm.IdeellaForeningar => "Ideella föreningar",
            OrganisationForm.Samfalligheter => "Samfälligheter",
            OrganisationForm.RegistreratTrossamfund => "Registrerat trossamfund",
            OrganisationForm.Familjestiftelser => "Familjestiftelser",
            OrganisationForm.StiftelserFonderOvriga => "Övriga stiftelser och fonder",
            OrganisationForm.StatligaEnheter => "Statliga enheter",
            OrganisationForm.Kommuner => "Kommuner",
            OrganisationForm.Kommunalforbund => "Kommunalförbund",
            OrganisationForm.Regioner => "Regioner",
            OrganisationForm.AllmannaForsakringskassor => "Allmänna försäkringskassor",
            OrganisationForm.OffentligaKorporationerAnstalter => "Offentliga korporationer och anstalter",
            OrganisationForm.Hypoteksforeningar => "Hypoteksföreningar",
            OrganisationForm.RegionalaStatligaMyndigheter => "Regionala statliga myndigheter",
            OrganisationForm.OskiftadeDodsbon => "Oskiftade dödsbon",
            OrganisationForm.OmsesidigaForsakringsbolag => "Ömsesidiga försäkringsbolag",
            OrganisationForm.Sparbanker => "Sparbanker",
            OrganisationForm.UnderstodsforeningarForsakringsforeningar =>
                "Understödsföreningar och Försäkringsföreningar",
            OrganisationForm.Arbetsloshetskassor => "Arbetslöshetskassor",
            OrganisationForm.UtlandskaJuridiskaPersoner => "Utländska juridiska personer",
            OrganisationForm.OvrigaSvenskaJuridiskaPersoner =>
                "Övriga svenska juridiska personer bildade enligt särskild lagstiftning",
            OrganisationForm.JuridiskFormEjUtredd => "Juridisk form ej utredd",
            // Defensive fallback for a forced cast of an undeclared int (e.g. (OrganisationForm)9999).
            // Per CA1065, property getters MUST NOT throw — return the unknown-form description instead.
            _ => "Juridisk form ej utredd"
        };

        /// <summary>Whether this form represents a legal person or a physical person.</summary>
        public OrganisationNumberType NumberType => form == OrganisationForm.None
            ? OrganisationNumberType.PhysicalPerson
            : OrganisationNumberType.LegalPerson;
    }
}
