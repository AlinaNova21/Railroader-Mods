using System;

namespace AlinasRailTools.Core.Attributes;

/// <summary>
/// Validates that a property has a non-null, non-empty value
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class RequiredAttribute : ValidationAttribute
{
    public override ValidationResult Validate(object value, IWorkingSnapshot snapshot, string propertyName)
    {
        if (value == null)
        {
            return ValidationResult.Failure($"{propertyName} is required but was null");
        }

        if (value is string str && string.IsNullOrWhiteSpace(str))
        {
            return ValidationResult.Failure($"{propertyName} is required but was empty or whitespace");
        }

        return ValidationResult.Success();
    }
}