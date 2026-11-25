namespace Civitas.Id.Sweden.Fakers.Internal;

/// <summary>A way to write a valid number.</summary>
internal enum IdNotation
{
    /// <summary>Twelve digits for a person, ten for a legal person.</summary>
    Canonical,

    /// <summary>ASCII spaces around the canonical form.</summary>
    AsciiPadded,

    /// <summary>No-break spaces around the canonical form.</summary>
    NoBreakSpacePadded,

    /// <summary>A legal person's number after the two digits <c>16</c>.</summary>
    SixteenPrefix,

    /// <summary>A legal person's number with the separator that means 100 years or more.</summary>
    PlusOnLegalPerson,

    /// <summary>
    ///     A person's number whose century says under 100 years and whose separator says over. Which birth years can
    ///     have it depends on the year it is written in.
    /// </summary>
    ContradictoryPlus,

    /// <summary>
    ///     Another legal person's number: the same one with <c>000</c> in positions 7–9 and the check digit that
    ///     matches.
    /// </summary>
    ZeroSerialLegalPerson,

    /// <summary>The country letters before the canonical form.</summary>
    SePrefix,

    /// <summary>
    ///     Spaces before the canonical form up to 101 characters, one over the length limit. The string and span entry
    ///     points trim before they apply the limit and so accept it; the UTF-8 entry points reject it.
    /// </summary>
    OverLengthPadded
}
