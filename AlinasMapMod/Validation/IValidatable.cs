using System.Collections.Generic;

namespace AlinasMapMod.Validation;

/// <summary>
/// Interface for objects that can be validated with both simple and detailed validation methods
/// </summary>
public interface IValidatable
{
    /// <summary>
    /// Simple validation that throws ValidationException on failure (maintains backward compatibility)
    /// </summary>
    void Validate();
    
    /// <summary>
    /// Rich validation that returns detailed results including errors and warnings
    /// </summary>
    ValidationResult ValidateWithDetails();
}