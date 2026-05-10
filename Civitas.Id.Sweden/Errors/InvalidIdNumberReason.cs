namespace Civitas.Id.Sweden.Errors;

/// <summary>
///     Discriminator describing why a Swedish ID number string failed to parse.
/// </summary>
public enum InvalidIdNumberReason
{
    /// <summary>Failure reason was not specified.</summary>
    Unknown = 0,

    /// <summary>The input was null, empty, or whitespace-only.</summary>
    Empty,

    /// <summary>The input length is outside any supported format.</summary>
    InvalidLength,

    /// <summary>The input did not match the expected pattern of digits and separators.</summary>
    InvalidFormat,

    /// <summary>The encoded date is not a real calendar date.</summary>
    InvalidDate,

    /// <summary>The Luhn check digit does not match the computed value.</summary>
    InvalidChecksum,

    /// <summary>The organisation form code was not recognised.</summary>
    UnknownOrganisationForm,

    /// <summary>The input did not match any supported Swedish ID type.</summary>
    UnsupportedIdType
}
