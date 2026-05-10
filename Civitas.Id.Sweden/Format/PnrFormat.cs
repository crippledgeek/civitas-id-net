namespace Civitas.Id.Sweden.Format;

/// <summary>
///     Output format options for Swedish personal identification numbers.
/// </summary>
public enum PnrFormat
{
    /// <summary>YYYYMMDDXXXX (12 digits, no separator).</summary>
    LongFormat,

    /// <summary>YYYYMMDD-XXXX or YYYYMMDD+XXXX depending on age (separator inferred).</summary>
    LongFormatWithSeparator,

    /// <summary>YYYYMMDD-XXXX with hyphen regardless of age.</summary>
    LongFormatWithStandardSeparator,

    /// <summary>YYMMDDXXXX (10 digits, no separator).</summary>
    ShortFormat,

    /// <summary>YYMMDD-XXXX or YYMMDD+XXXX depending on age (separator inferred).</summary>
    ShortFormatWithSeparator,

    /// <summary>YYMMDD-XXXX with hyphen regardless of age.</summary>
    ShortFormatWithStandardSeparator
}
