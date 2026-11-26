using System.ComponentModel.DataAnnotations;
using Civitas.Id.Sweden.Core;

namespace Civitas.Id.Sweden.DataAnnotations;

/// <summary>
///     The rule behind the four <c>Valid*Id</c> attributes: null passes, a string passes when it parses, an instance
///     passes when it is a <typeparamref name="T" />, and anything else fails with the attribute's own message.
/// </summary>
/// <remarks>
///     A <see cref="ValidationContext" /> is optional. <see cref="ValidationAttribute.IsValid(object)" /> calls the
///     context overload with <see langword="null" /> and reads only whether the result is
///     <see cref="ValidationResult.Success" />, so a failure without a context carries no message.
/// </remarks>
/// <typeparam name="T">The ID type the attribute accepts as an instance.</typeparam>
internal sealed class IdAttributeRule<T>
    where T : SwedishOfficialId
{
    private const string FallbackName = "value";

    private readonly Func<string, bool> _parses;
    private readonly string _expected;

    /// <summary>Creates a rule.</summary>
    /// <param name="parses">Whether a string is a number of the expected kind.</param>
    /// <param name="expected">
    ///     The expected type with its article, as in "a PersonalId", for the type-mismatch message. It is message
    ///     prose with no invariant, and "a Swedish official ID" cannot be derived from a type name.
    /// </param>
    internal IdAttributeRule(Func<string, bool> parses, string expected)
    {
        _parses = parses;
        _expected = expected;
    }

    /// <summary>Judges <paramref name="value" /> for <paramref name="attribute" />.</summary>
    internal ValidationResult? Evaluate(object? value, ValidationContext? validationContext, ValidationAttribute attribute)
    {
        return value switch
        {
            null => ValidationResult.Success,
            string text when _parses(text) => ValidationResult.Success,
            string => Rejected(validationContext, attribute),
            T => ValidationResult.Success,
            _ => Mismatch(value, validationContext)
        };
    }

    private static ValidationResult Rejected(ValidationContext? validationContext, ValidationAttribute attribute)
    {
        return validationContext is null
            ? new ValidationResult(null)
            : new ValidationResult(attribute.FormatErrorMessage(validationContext.MemberName ?? FallbackName));
    }

    private ValidationResult Mismatch(object value, ValidationContext? validationContext)
    {
        return validationContext is null
            ? new ValidationResult(null)
            : new ValidationResult($"Type {value.GetType().Name} is not {_expected}.");
    }
}
