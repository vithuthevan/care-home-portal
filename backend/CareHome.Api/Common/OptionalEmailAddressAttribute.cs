using System.ComponentModel.DataAnnotations;

namespace CareHome.Api.Common;

/// <summary>
/// Validates email format when a value is present; null and whitespace are allowed.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class OptionalEmailAddressAttribute : ValidationAttribute
{
    private static readonly EmailAddressAttribute Inner = new();

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success;
        }

        if (value is string text && string.IsNullOrWhiteSpace(text))
        {
            return ValidationResult.Success;
        }

        return Inner.GetValidationResult(value, validationContext);
    }
}
