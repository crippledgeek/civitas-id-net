using System.ComponentModel.DataAnnotations;
using Civitas.Id.Sweden.Core;

namespace Civitas.Id.Sweden.DataAnnotations;

/// <summary>
///     The rule behind the four <c>Valid*Id</c> attributes: null passes, a string passes when it parses, an instance
///     passes when it is a <typeparamref name="T" />, and anything else fails with the attribute's own error message.
/// </summary>
/// <remarks>
///     A <see cref="ValidationContext" /> is optional. <see cref="ValidationAttribute.IsValid(object)" /> calls the
///     context overload with <see langword="null" /> and reads only whether the result is
///     <see cref="ValidationResult.Success" />, so a failure without a context carries no message. With a context a
///     failure names the field by <see cref="ValidationContext.DisplayName" /> and carries
///     <see cref="ValidationContext.MemberName" />, as the base class does for its own failures.
/// </remarks>
/// <typeparam name="T">The ID type the attribute accepts as an instance.</typeparam>
internal sealed class IdAttributeRule<T>
    where T : SwedishOfficialId
{
    private readonly Func<string, bool> _parses;

    /// <summary>Creates a rule.</summary>
    /// <param name="parses">Whether a string is a number of the expected kind.</param>
    internal IdAttributeRule(Func<string, bool> parses)
    {
        _parses = parses;
    }

    /// <summary>Judges <paramref name="value" /> for <paramref name="attribute" />.</summary>
    internal ValidationResult? Evaluate(object? value, ValidationContext? validationContext, ValidationAttribute attribute)
    {
        return value switch
        {
            null => ValidationResult.Success,
            string text when _parses(text) => ValidationResult.Success,
            T => ValidationResult.Success,
            _ => Rejected(validationContext, attribute)
        };
    }

    private static ValidationResult Rejected(ValidationContext? validationContext, ValidationAttribute attribute)
    {
        if (validationContext is null) return new ValidationResult(null);

        var message = attribute.FormatErrorMessage(validationContext.DisplayName);
        return validationContext.MemberName is { } member
            ? new ValidationResult(message, [member])
            : new ValidationResult(message);
    }
}
