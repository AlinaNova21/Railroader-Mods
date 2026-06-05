using System;

namespace AlinasRailTools.Core.Attributes;

/// <summary>
/// Validates that a numeric property falls within a specified range
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class RangeAttribute : ValidationAttribute
{
    private readonly double _minimum;
    private readonly double _maximum;

    /// <summary>
    /// Create a range validator
    /// </summary>
    /// <param name="minimum">Minimum allowed value (inclusive)</param>
    /// <param name="maximum">Maximum allowed value (inclusive)</param>
    public RangeAttribute(double minimum, double maximum)
    {
        if (minimum > maximum)
        {
            throw new ArgumentException("Minimum cannot be greater than maximum");
        }
        
        _minimum = minimum;
        _maximum = maximum;
    }

    /// <summary>
    /// Create a range validator with integer bounds
    /// </summary>
    /// <param name="minimum">Minimum allowed value (inclusive)</param>
    /// <param name="maximum">Maximum allowed value (inclusive)</param>
    public RangeAttribute(int minimum, int maximum) : this((double)minimum, (double)maximum)
    {
    }

    public override ValidationResult Validate(object value, IWorkingSnapshot snapshot, string propertyName)
    {
        if (value == null)
        {
            return ValidationResult.Success(); // null values are allowed unless also marked with Required
        }

        double numericValue;
        
        try
        {
            numericValue = Convert.ToDouble(value);
        }
        catch (Exception)
        {
            return ValidationResult.Failure($"{propertyName} must be a numeric value for range validation");
        }

        if (numericValue < _minimum || numericValue > _maximum)
        {
            return ValidationResult.Failure($"{propertyName} value {numericValue} is outside the allowed range [{_minimum}, {_maximum}]");
        }

        return ValidationResult.Success();
    }
}