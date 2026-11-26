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
    private static readonly IdAttributeRule<CoordinationId> Rule = new(CoordinationId.IsValid, "a CoordinationId");

    /// <inheritdoc />
    protected override ValidationResult? IsValid(object? value, ValidationContext? validationContext)
    {
        return Rule.Evaluate(value, validationContext, this);
    }

    /// <inheritdoc />
    public override string FormatErrorMessage(string name)
    {
        return $"The {name} field is not a valid Swedish samordningsnummer.";
    }
}
