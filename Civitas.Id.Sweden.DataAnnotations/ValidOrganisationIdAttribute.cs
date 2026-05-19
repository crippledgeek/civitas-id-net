using System.ComponentModel.DataAnnotations;
using Civitas.Id.Sweden.Core;
using JetBrains.Annotations;

namespace Civitas.Id.Sweden.DataAnnotations;

/// <summary>
///     Validates that a value is a Swedish <see cref="OrganisationId" /> (organisationsnummer).
///     Accepts both <see cref="string" /> and <see cref="OrganisationId" /> values;
///     <see langword="null" /> passes.
/// </summary>
/// <remarks>
///     <para>
///         By DataAnnotations convention, <see langword="null" /> short-circuits to
///         <see cref="ValidationResult.Success" />. Combine with
///         <see cref="RequiredAttribute" /> to reject null inputs.
///     </para>
///     <para>
///         Note: a 12-digit personnummer string may also pass this check when it
///         denotes an Enskild firma (sole proprietor) per Lag (1974:174). Use
///         <see cref="ValidPersonalIdAttribute" /> to require strictly the
///         personnummer subtype on a string input.
///     </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
[PublicAPI]
public sealed class ValidOrganisationIdAttribute : ValidationAttribute
{
    /// <inheritdoc />
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        ArgumentNullException.ThrowIfNull(validationContext);
        if (value is null) return ValidationResult.Success;
        return value switch
        {
            string s when OrganisationId.TryParse(s, out _) => ValidationResult.Success,
            OrganisationId => ValidationResult.Success,
            string => new ValidationResult(FormatErrorMessage(validationContext.MemberName ?? "value")),
            _ => new ValidationResult($"Type {value.GetType().Name} is not an OrganisationId.")
        };
    }

    /// <inheritdoc />
    public override string FormatErrorMessage(string name)
    {
        return $"The {name} field is not a valid Swedish organisationsnummer.";
    }
}
