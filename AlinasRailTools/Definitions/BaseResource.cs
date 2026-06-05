using System;
using System.Collections.Generic;
using System.Reflection;
using AlinasRailTools.Core;
using AlinasRailTools.Core.Attributes;

namespace AlinasRailTools.Definitions;

/// <summary>
/// Base class for all ART resources providing common identification and validation functionality.
/// </summary>
public abstract class BaseResource : IResource
{
    /// <summary>
    /// Unique identifier for this resource
    /// </summary>
    [Required]
    public string Id { get; set; } = "";

    /// <summary>
    /// Validate this resource using attribute-based validation
    /// </summary>
    /// <param name="snapshot">Working snapshot for dependency validation</param>
    /// <returns>Validation result</returns>
    public virtual ValidationResult Validate(IWorkingSnapshot snapshot)
    {
        var errors = new List<string>();

        // Validate all properties using their validation attributes
        var properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        
        foreach (var property in properties)
        {
            var validationAttrs = property.GetCustomAttributes(typeof(ValidationAttribute), true);
            
            foreach (ValidationAttribute validationAttr in validationAttrs)
            {
                var propertyValue = property.GetValue(this);
                var result = validationAttr.Validate(propertyValue, snapshot, property.Name);
                
                if (!result.IsValid)
                {
                    errors.AddRange(result.Errors);
                }
            }
        }

        // Allow derived classes to add custom validation logic
        var customResult = ValidateCustom(snapshot);
        if (!customResult.IsValid)
        {
            errors.AddRange(customResult.Errors);
        }

        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors);
    }

    /// <summary>
    /// Override this method to add custom validation logic beyond what attributes can provide
    /// </summary>
    /// <param name="snapshot">Working snapshot for dependency validation</param>
    /// <returns>Custom validation result</returns>
    protected virtual ValidationResult ValidateCustom(IWorkingSnapshot snapshot)
    {
        return ValidationResult.Success();
    }
}