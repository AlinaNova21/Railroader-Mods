using System;
using System.Linq;
using System.Reflection;
using Serilog;

namespace AlinasRailTools.Core.Attributes;

/// <summary>
/// Validates that a property references a valid resource in the working snapshot
/// Automatically infers the resource section from the ResourceType attribute on the target resource class
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ResourceReferenceAttribute : ValidationAttribute
{
    private static readonly ILogger Logger = Log.ForContext<ResourceReferenceAttribute>();

    private readonly Type _resourceType;
    private readonly string _sectionName;

    /// <summary>
    /// Create a resource reference validator that auto-infers the section name from ResourceType attribute
    /// </summary>
    /// <param name="resourceType">The type of resource being referenced</param>
    public ResourceReferenceAttribute(Type resourceType)
    {
        _resourceType = resourceType ?? throw new ArgumentNullException(nameof(resourceType));
        
        // Auto-infer section name from ResourceType attribute
        var resourceTypeAttr = resourceType.GetCustomAttributes(typeof(ResourceTypeAttribute), false)
            .Cast<ResourceTypeAttribute>()
            .FirstOrDefault();
            
        if (resourceTypeAttr == null)
        {
            throw new InvalidOperationException($"Resource type {resourceType.Name} must have a ResourceType attribute to use ResourceReference auto-inference");
        }
        
        _sectionName = resourceTypeAttr.SectionName;
    }

    /// <summary>
    /// Create a resource reference validator with explicit section name (for backward compatibility)
    /// </summary>
    /// <param name="resourceType">The type of resource being referenced</param>
    /// <param name="sectionName">Explicit section name to use</param>
    public ResourceReferenceAttribute(Type resourceType, string sectionName)
    {
        _resourceType = resourceType ?? throw new ArgumentNullException(nameof(resourceType));
        _sectionName = sectionName ?? throw new ArgumentNullException(nameof(sectionName));
    }

    public override ValidationResult Validate(object value, IWorkingSnapshot snapshot, string propertyName)
    {
        if (value == null)
        {
            return ValidationResult.Success(); // null references are allowed unless also marked with Required
        }

        if (!(value is string resourceId))
        {
            return ValidationResult.Failure($"{propertyName} must be a string resource ID");
        }

        if (string.IsNullOrWhiteSpace(resourceId))
        {
            return ValidationResult.Success(); // empty references are allowed unless also marked with Required
        }

        Logger.Debug("Validating resource reference {PropertyName} = '{ResourceId}' in section '{SectionName}'",
            propertyName, resourceId, _sectionName);

        if (!snapshot.ResourceExists(_sectionName, resourceId))
        {
            var errorMessage = $"{propertyName} references '{resourceId}' but no {_resourceType.Name} with that ID exists in section '{_sectionName}'";
            Logger.Warning("Resource reference validation failed: {ErrorMessage}", errorMessage);
            return ValidationResult.Failure(errorMessage);
        }

        return ValidationResult.Success();
    }
}