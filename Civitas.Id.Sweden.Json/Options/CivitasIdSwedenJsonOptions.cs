using System.ComponentModel.DataAnnotations;
using Civitas.Id.Sweden.Format;
using JetBrains.Annotations;

namespace Civitas.Id.Sweden.Json.Options;

/// <summary>Options for the Civitas.Id.Sweden System.Text.Json integration.</summary>
[PublicAPI]
public sealed class CivitasIdSwedenJsonOptions
{
    /// <summary>
    /// The format used when serializing personnummer / samordningsnummer.
    /// Defaults to <see cref="PnrFormat.LongFormat"/>.
    /// </summary>
    [EnumDataType(typeof(PnrFormat))]
    public PnrFormat PersonnummerFormat { get; set; } = PnrFormat.LongFormat;

    /// <summary>
    /// When <see langword="true"/> (default), organisation numbers are serialized
    /// in their 12-digit form for Enskild firma (legal persons stay 10-digit).
    /// </summary>
    public bool OrganisationIdLongFormat { get; set; } = true;
}
