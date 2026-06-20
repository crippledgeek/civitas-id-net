using System.Text.Json.Serialization;

namespace Civitas.Id.Sweden.Format;

/// <summary>
///     Swedish organisation legal forms ("organisationsform").
///     The form code is encoded in the first two digits of the organisation number.
///     Source: Bolagsverket. Underlying integer value of each member is the form code.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<OrganisationForm>))]
public enum OrganisationForm
{
    /// <summary>Ingen organisationsform — Fysisk person (sole proprietor).</summary>
    None = 0,

    /// <summary>Enkla bolag (simple companies).</summary>
    EnklaBolag = 21,

    /// <summary>Partrederier (shipping partnerships).</summary>
    Partrederier = 22,

    /// <summary>Handelsbolag, kommanditbolag (trading / limited partnerships).</summary>
    HandelsbolagKommanditbolag = 31,

    /// <summary>Gruvbolag (mining companies).</summary>
    Gruvbolag = 32,

    /// <summary>Bankaktiebolag (banking companies).</summary>
    Bankaktiebolag = 41,

    /// <summary>Försäkringsaktiebolag (insurance companies).</summary>
    Forsakringsaktiebolag = 42,

    /// <summary>Europabolag (European companies — SE).</summary>
    Europabolag = 43,

    /// <summary>Övriga aktiebolag (other limited companies).</summary>
    AktiebolagOvriga = 49,

    /// <summary>Ekonomiska föreningar (economic associations).</summary>
    EkonomiskaForeningar = 51,

    /// <summary>Bostadsrättsföreningar (tenant-ownership associations).</summary>
    Bostadsrattsforeningar = 53,

    /// <summary>Kooperativ Hyresrättsförening (cooperative rental associations).</summary>
    KooperativHyresrattsforening = 54,

    /// <summary>Europakooperativ, EGTS och Eric-konsortier (form code 55).</summary>
    /// <remarks>
    ///     NOTE: orgnummer in the sub-ranges <c>556…</c> and <c>559…</c> have first
    ///     two digits <c>55</c> but are <see cref="AktiebolagOvriga" /> (form 49),
    ///     not Europakooperativ. Bolagsverket announced 2015-01-12 that newly issued
    ///     Aktiebolag use the <c>559…</c> prefix because the historical <c>556…</c>
    ///     range was exhausted (last <c>556999-9997</c>, first <c>559000-0005</c>).
    ///     <see cref="Civitas.Id.Sweden.Core.OrganisationId.Form" /> handles this
    ///     carve-out automatically.
    /// </remarks>
    EuropakooperativEgtsEric = 55,

    /// <summary>Ideella föreningar (non-profit associations).</summary>
    IdeellaForeningar = 61,

    /// <summary>Samfälligheter (joint property management associations).</summary>
    Samfalligheter = 62,

    /// <summary>Registrerat trossamfund (registered religious communities).</summary>
    RegistreratTrossamfund = 63,

    /// <summary>Familjestiftelser (family foundations).</summary>
    Familjestiftelser = 71,

    /// <summary>Övriga stiftelser och fonder (other foundations and funds).</summary>
    StiftelserFonderOvriga = 72,

    /// <summary>Statliga enheter (state entities).</summary>
    StatligaEnheter = 81,

    /// <summary>Kommuner (municipalities).</summary>
    Kommuner = 82,

    /// <summary>Kommunalförbund (municipal federations).</summary>
    Kommunalforbund = 83,

    /// <summary>Regioner (regions).</summary>
    Regioner = 84,

    /// <summary>Allmänna försäkringskassor (general insurance funds).</summary>
    AllmannaForsakringskassor = 85,

    /// <summary>Offentliga korporationer och anstalter.</summary>
    OffentligaKorporationerAnstalter = 87,

    /// <summary>Hypoteksföreningar (mortgage associations).</summary>
    Hypoteksforeningar = 88,

    /// <summary>Regionala statliga myndigheter (regional state authorities).</summary>
    RegionalaStatligaMyndigheter = 89,

    /// <summary>Oskiftade dödsbon (undivided estates).</summary>
    OskiftadeDodsbon = 91,

    /// <summary>Ömsesidiga försäkringsbolag (mutual insurance companies).</summary>
    OmsesidigaForsakringsbolag = 92,

    /// <summary>Sparbanker (savings banks).</summary>
    Sparbanker = 93,

    /// <summary>Understödsföreningar och Försäkringsföreningar.</summary>
    UnderstodsforeningarForsakringsforeningar = 94,

    /// <summary>Arbetslöshetskassor (unemployment insurance funds).</summary>
    Arbetsloshetskassor = 95,

    /// <summary>Utländska juridiska personer (foreign legal entities).</summary>
    UtlandskaJuridiskaPersoner = 96,

    /// <summary>Övriga svenska juridiska personer bildade enligt särskild lagstiftning.</summary>
    OvrigaSvenskaJuridiskaPersoner = 98,

    /// <summary>Juridisk form ej utredd (legal form not determined).</summary>
    JuridiskFormEjUtredd = 99
}
