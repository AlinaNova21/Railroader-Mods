using System;

namespace AlinasMapMod.Validation;

/// <summary>
/// Base class for validation rules that can validate values of type T
/// </summary>
public abstract class ValidationRule<T>
{
    public abstract ValidationResult Validate(T value, ValidationContext context);
}

/// <summary>
/// Custom validation rule using a delegate function
/// </summary>
public class CustomRule<T> : ValidationRule<T>
{
    private readonly Func<T, ValidationContext, ValidationResult> _validator;
    
    public CustomRule(Func<T, ValidationContext, ValidationResult> validator)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }
    
    public override ValidationResult Validate(T value, ValidationContext context)
    {
        return _validator(value, context);
    }
}