using System.ComponentModel.DataAnnotations;
using Civitas.Id.Sweden.Core;
using JetBrains.Annotations;

namespace Civitas.Id.Sweden.DataAnnotations;

/// <summary>
///     Validates that a value is a Swedish <see cref="CoordinationId" /> (samordningsnummer).
///     Accepts both <see cref="string" /> and <see cref="CoordinationId" /> values;
///     <see langword="null" /> passes.
/// </summary>
/// <remarks>
///     By DataAnnotations convention, <see langword="null" /> short-circuits to
///     <see cref="ValidationResult.Success" />. Combine with
///     <see cref="RequiredAttribute" /> to reject null inputs.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
[PublicAPI]
public sealed class ValidCoordinationIdAttribute : ValidationAttribute
{
    /// <inheritdoc />
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        ArgumentNullException.ThrowIfNull(validationContext);
        if (value is null) return ValidationResult.Success;
        return value switch
        {
            string s when CoordinationId.TryParse(s, out _) => ValidationResult.Success,
            CoordinationId => ValidationResult.Success,
            string => new ValidationResult(FormatErrorMessage(validationContext.MemberName ?? "value")),
            _ => new ValidationResult($"Type {value.GetType().Name} is not a CoordinationId.")
        };
    }

    /// <inheritdoc />
    public override string FormatErrorMessage(string name)
    {
        return $"The {name} field is not a valid Swedish samordningsnummer.";
    }
}
