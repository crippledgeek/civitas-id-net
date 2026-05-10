using JetBrains.Annotations;

namespace Civitas.Id.Sweden.Errors;

/// <summary>
///     Thrown when a Swedish official ID number cannot be parsed.
///     Derives from <see cref="FormatException" /> so callers can use BCL-shaped catch blocks.
/// </summary>
/// <remarks>
///     <para>
///         The raw offending input is intentionally NOT stored on this exception.
///         Personnummer, samordningsnummer, and organisationsnummer are personal
///         data under GDPR (Article 9 / Swedish Dataskyddslagen 2018:218). Storing
///         the raw value risks accidental capture by structured-log shippers,
///         crash reporters (Sentry, Application Insights, Datadog), and the
///         default ASP.NET Core exception middleware — all of which capture
///         <see cref="System.Exception.Message" /> verbatim.
///     </para>
///     <para>
///         For diagnostic correlation, <see cref="RedactedInput" /> exposes a
///         partially-redacted form of the input (first two characters + asterisks
///         + last four characters). This is non-recoverable but sufficient to
///         distinguish formats and to correlate failures across logs.
///     </para>
/// </remarks>
[PublicAPI]
public sealed class InvalidIdNumberException : FormatException
{
    /// <summary>Initialises a new exception with a default message and unknown reason.</summary>
    public InvalidIdNumberException()
        : base(BuildMessage(InvalidIdNumberReason.Unknown))
    {
        RedactedInput = null;
        Reason = InvalidIdNumberReason.Unknown;
    }

    /// <summary>
    ///     Initialises a new exception wrapping an inner exception with the default
    ///     message and unknown reason.
    /// </summary>
    /// <param name="innerException">The underlying cause.</param>
    public InvalidIdNumberException(Exception innerException)
        : base(BuildMessage(InvalidIdNumberReason.Unknown), innerException)
    {
        RedactedInput = null;
        Reason = InvalidIdNumberReason.Unknown;
    }

    /// <summary>
    ///     Initialises a new exception with an explicit message. Provided for
    ///     BCL exception-constructor convention (CA1032). Callers MUST NOT include
    ///     the raw offending ID in <paramref name="message" /> — see class remarks.
    /// </summary>
    /// <param name="message">The exception message. Must not contain raw PII.</param>
    public InvalidIdNumberException(string message)
        : base(message)
    {
        RedactedInput = null;
        Reason = InvalidIdNumberReason.Unknown;
    }

    /// <summary>
    ///     Initialises a new exception with an explicit message and inner exception.
    ///     Provided for BCL exception-constructor convention (CA1032). Callers MUST
    ///     NOT include the raw offending ID in <paramref name="message" /> — see
    ///     class remarks.
    /// </summary>
    /// <param name="message">The exception message. Must not contain raw PII.</param>
    /// <param name="innerException">The underlying cause.</param>
    public InvalidIdNumberException(string message, Exception innerException)
        : base(message, innerException)
    {
        RedactedInput = null;
        Reason = InvalidIdNumberReason.Unknown;
    }

    /// <summary>
    ///     Initialises a new exception with full parse-failure context.
    /// </summary>
    /// <param name="input">
    ///     The offending raw input. The exception stores only a redacted form;
    ///     the raw value is never copied to <see cref="System.Exception.Message" />
    ///     or any other public surface.
    /// </param>
    /// <param name="reason">The specific validation failure.</param>
    public InvalidIdNumberException(string? input, InvalidIdNumberReason reason)
        : base(BuildMessage(reason))
    {
        RedactedInput = Redact(input);
        Reason = reason;
    }

    /// <summary>
    ///     Initialises a new exception with full parse-failure context and an inner exception.
    /// </summary>
    /// <param name="input">
    ///     The offending raw input. The exception stores only a redacted form;
    ///     the raw value is never copied to <see cref="System.Exception.Message" />
    ///     or any other public surface.
    /// </param>
    /// <param name="reason">The specific validation failure.</param>
    /// <param name="innerException">The underlying cause.</param>
    public InvalidIdNumberException(string? input, InvalidIdNumberReason reason, Exception innerException)
        : base(BuildMessage(reason), innerException)
    {
        RedactedInput = Redact(input);
        Reason = reason;
    }

    /// <summary>
    ///     Gets a partially-redacted representation of the offending input for
    ///     diagnostic correlation. The raw value is never stored.
    /// </summary>
    /// <remarks>
    ///     Format: first two characters + asterisks for the middle + last four
    ///     characters, or a stable placeholder for null / empty / very-short
    ///     inputs. The redaction is non-recoverable; correlations are based on
    ///     length and prefix/suffix only.
    /// </remarks>
    public string? RedactedInput { get; }

    /// <summary>Gets the specific reason the input was rejected.</summary>
    public InvalidIdNumberReason Reason { get; }

    private static string BuildMessage(InvalidIdNumberReason reason)
    {
        return reason switch
        {
            InvalidIdNumberReason.Empty => "Invalid Swedish ID number: input is empty.",
            InvalidIdNumberReason.InvalidLength => "Invalid Swedish ID number: incorrect length.",
            InvalidIdNumberReason.InvalidFormat => "Invalid Swedish ID number: malformed input.",
            InvalidIdNumberReason.InvalidDate => "Invalid Swedish ID number: invalid date component.",
            InvalidIdNumberReason.InvalidChecksum => "Invalid Swedish ID number: incorrect Luhn checksum.",
            InvalidIdNumberReason.UnknownOrganisationForm =>
                "Invalid Swedish ID number: unrecognised organisation form.",
            InvalidIdNumberReason.UnsupportedIdType =>
                "Invalid Swedish ID number: input does not match any supported ID type.",
            _ => "Invalid Swedish ID number."
        };
    }

    private static string? Redact(string? input)
    {
        return input?.Length switch
        {
            null => null,
            0 => "[empty]",
            <= 6 => new string('*', input.Length),
            // Keep first 2 + last 4 chars visible, mask middle.
            _ => string.Create(input.Length, input, static (span, s) =>
            {
                s.AsSpan(0, 2).CopyTo(span);
                span[2..^4].Fill('*');
                s.AsSpan(s.Length - 4).CopyTo(span[^4..]);
            })
        };
    }
}
