using System.Collections.Generic;
using System.Linq;

namespace AlinasRailTools.Core;

/// <summary>
/// Result of a resource validation operation
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new List<string>();
    
    public ValidationResult(bool isValid, List<string> errors = null)
    {
        IsValid = isValid;
        Errors = errors ?? new List<string>();
    }
    
    public static ValidationResult Success() => new(true);
    
    public static ValidationResult Failure(params string[] errors) => new(false, errors.ToList());
    
    public static ValidationResult Failure(List<string> errors) => new(false, errors);
}