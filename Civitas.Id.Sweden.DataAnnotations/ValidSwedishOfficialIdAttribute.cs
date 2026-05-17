using System.ComponentModel.DataAnnotations;
using Civitas.Id.Sweden.Core;
using JetBrains.Annotations;

namespace Civitas.Id.Sweden.DataAnnotations;

/// <summary>
///     Validates that a value is a Swedish official ID of any kind — personnummer,
///     samordningsnummer, or organisationsnummer. Accepts both <see cref="string" />
///     and <see cref="SwedishOfficialId" /> values; <see langword="null" /> passes.
/// </summary>
/// <remarks>
///     By DataAnnotations convention, <see langword="null" /> short-circuits to
///     <see cref="ValidationResult.Success" />. Combine with
///     <see cref="RequiredAttribute" /> to reject null inputs.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
[PublicAPI]
public sealed class ValidSwedishOfficialIdAttribute : ValidationAttribute
{
    /// <inheritdoc />
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        ArgumentNullException.ThrowIfNull(validationContext);
        if (value is null) return ValidationResult.Success;
        return value switch
        {
            string s when SwedishOfficialId.TryParseAny(s, out _) => ValidationResult.Success,
            SwedishOfficialId => ValidationResult.Success,
            string => new ValidationResult(FormatErrorMessage(validationContext.MemberName ?? "value")),
            _ => new ValidationResult($"Type {value.GetType().Name} is not a Swedish official ID.")
        };
    }

    /// <inheritdoc />
    public override string FormatErrorMessage(string name)
    {
        return $"The {name} field is not a valid Swedish official ID.";
    }
}
