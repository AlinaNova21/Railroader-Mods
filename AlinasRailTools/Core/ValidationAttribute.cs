using System;

namespace AlinasRailTools.Core;

/// <summary>
/// Base class for all validation attributes
/// </summary>
public abstract class ValidationAttribute : Attribute
{
    /// <summary>
    /// Validate a property value
    /// </summary>
    /// <param name="value">The property value to validate</param>
    /// <param name="snapshot">The working snapshot for resource reference validation</param>
    /// <param name="propertyName">The name of the property being validated</param>
    /// <returns>Validation result</returns>
    public abstract ValidationResult Validate(object value, IWorkingSnapshot snapshot, string propertyName);
}