using System.Collections.Generic;
using System.Linq;

namespace AlinasMapMod.Validation;

/// <summary>
/// Result of validation containing errors and warnings with detailed information
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; } = true;
    public List<ValidationError> Errors { get; set; } = new List<ValidationError>();
    public List<ValidationWarning> Warnings { get; set; } = new List<ValidationWarning>();
    
    /// <summary>
    /// Throws ValidationException if validation failed
    /// </summary>
    public void ThrowIfInvalid()
    {
        if (!IsValid)
        {
            var errorMessages = Errors.Select(e => e.Message);
            throw new ValidationException(string.Join("; ", errorMessages));
        }
    }
    
    /// <summary>
    /// Combines multiple validation results
    /// </summary>
    public static ValidationResult Combine(params ValidationResult[] results)
    {
        var combined = new ValidationResult { IsValid = true };
        
        if (results == null || results.Length == 0)
            return combined;
        
        foreach (var result in results)
        {
            if (result == null)
                continue;
                
            if (!result.IsValid)
                combined.IsValid = false;
                
            combined.Errors.AddRange(result.Errors);
            combined.Warnings.AddRange(result.Warnings);
        }
        
        return combined;
    }
}

/// <summary>
/// Represents a validation error with context information
/// </summary>
public class ValidationError
{
    public string Field { get; set; }
    public string Message { get; set; }
    public string Code { get; set; }
    public object Value { get; set; }
    
    public override string ToString()
    {
        return string.IsNullOrEmpty(Field) ? Message : $"{Field}: {Message}";
    }
}

/// <summary>
/// Represents a validation warning with context information
/// </summary>
public class ValidationWarning
{
    public string Field { get; set; }
    public string Message { get; set; }
    public object Value { get; set; }
    
    public override string ToString()
    {
        return string.IsNullOrEmpty(Field) ? Message : $"{Field}: {Message}";
    }
}