using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AlinasRailTools.Core.Attributes;
using AlinasRailTools.Definitions;
using Serilog;

namespace AlinasRailTools.Core;

/// <summary>
/// Registry for managing resource types and their metadata using reflection
/// </summary>
public class ResourceRegistry
{
    private static readonly ILogger Logger = Log.ForContext<ResourceRegistry>();
    private static readonly Lazy<ResourceRegistry> _instance = new Lazy<ResourceRegistry>(() => new ResourceRegistry());

    /// <summary>
    /// Get the singleton instance of the ResourceRegistry
    /// </summary>
    public static ResourceRegistry Instance => _instance.Value;

    private readonly Dictionary<string, Type> _sectionToType;
    private readonly Dictionary<Type, string> _typeToSection;
    private readonly Dictionary<string, ResourceTypeInfo> _resourceTypes;

    private ResourceRegistry()
    {
        _sectionToType = new Dictionary<string, Type>();
        _typeToSection = new Dictionary<Type, string>();
        _resourceTypes = new Dictionary<string, ResourceTypeInfo>();
        
        ScanAssemblyForResourceTypes();
    }

    /// <summary>
    /// Information about a registered resource type
    /// </summary>
    public class ResourceTypeInfo
    {
        public Type Type { get; set; }
        public string SectionName { get; set; }
        public ResourceTypeAttribute Attribute { get; set; }
        public bool IsBaseResource { get; set; }
    }

    /// <summary>
    /// Get the resource type for a given section name
    /// </summary>
    /// <param name="sectionName">Section name (e.g., "trackNodes")</param>
    /// <returns>Resource type, or null if not found</returns>
    public Type GetResourceType(string sectionName)
    {
        return _sectionToType.TryGetValue(sectionName, out var type) ? type : null;
    }

    /// <summary>
    /// Get the section name for a given resource type
    /// </summary>
    /// <param name="resourceType">Resource type</param>
    /// <returns>Section name, or null if not found</returns>
    public string GetSectionName(Type resourceType)
    {
        return _typeToSection.TryGetValue(resourceType, out var sectionName) ? sectionName : null;
    }

    /// <summary>
    /// Get information about a resource type by section name
    /// </summary>
    /// <param name="sectionName">Section name</param>
    /// <returns>Resource type info, or null if not found</returns>
    public ResourceTypeInfo GetResourceTypeInfo(string sectionName)
    {
        return _resourceTypes.TryGetValue(sectionName, out var info) ? info : null;
    }

    /// <summary>
    /// Get all registered resource types
    /// </summary>
    /// <returns>Dictionary of section name to resource type info</returns>
    public Dictionary<string, ResourceTypeInfo> GetAllResourceTypes()
    {
        return new Dictionary<string, ResourceTypeInfo>(_resourceTypes);
    }

    /// <summary>
    /// Check if a section name is registered
    /// </summary>
    /// <param name="sectionName">Section name to check</param>
    /// <returns>True if registered</returns>
    public bool IsValidSectionName(string sectionName)
    {
        return _sectionToType.ContainsKey(sectionName);
    }

    /// <summary>
    /// Create an instance of a resource type by section name
    /// </summary>
    /// <param name="sectionName">Section name</param>
    /// <param name="id">Resource ID</param>
    /// <returns>Resource instance, or null if section not found</returns>
    public IResource CreateResource(string sectionName, string id = "")
    {
        var type = GetResourceType(sectionName);
        if (type == null)
        {
            return null;
        }

        var resource = (IResource)Activator.CreateInstance(type);
        if (!string.IsNullOrEmpty(id))
        {
            resource.Id = id;
        }
        
        return resource;
    }

    /// <summary>
    /// Scan the current assembly for resource types with ResourceType attributes
    /// </summary>
    private void ScanAssemblyForResourceTypes()
    {
        Logger.Debug("Scanning assembly for resource types");

        var assembly = Assembly.GetExecutingAssembly();
        var resourceTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => typeof(IResource).IsAssignableFrom(t))
            .Where(t => t.GetCustomAttribute<ResourceTypeAttribute>() != null)
            .ToList();

        Logger.Information("Found {ResourceTypeCount} resource types in assembly", resourceTypes.Count);

        foreach (var type in resourceTypes)
        {
            var attribute = type.GetCustomAttribute<ResourceTypeAttribute>();
            var sectionName = attribute.SectionName;

            // Check for conflicts
            if (_sectionToType.ContainsKey(sectionName))
            {
                var conflictMessage = $"Multiple resource types registered for section '{sectionName}': {_sectionToType[sectionName].Name} and {type.Name}";
                Logger.Error(conflictMessage);
                throw new InvalidOperationException(conflictMessage);
            }

            // Register the mapping
            _sectionToType[sectionName] = type;
            _typeToSection[type] = sectionName;

            var info = new ResourceTypeInfo
            {
                Type = type,
                SectionName = sectionName,
                Attribute = attribute,
                IsBaseResource = type.IsSubclassOf(typeof(BaseResource))
            };

            _resourceTypes[sectionName] = info;

            Logger.Debug("Registered resource type {TypeName} for section {SectionName}", type.Name, sectionName);
        }
    }

    /// <summary>
    /// Manually register a resource type (for testing or external types)
    /// </summary>
    /// <param name="resourceType">Resource type to register</param>
    /// <param name="sectionName">Section name for this type</param>
    public void RegisterResourceType(Type resourceType, string sectionName)
    {
        if (!typeof(IResource).IsAssignableFrom(resourceType))
        {
            throw new ArgumentException($"Type {resourceType.Name} must implement IResource");
        }

        if (_sectionToType.ContainsKey(sectionName))
        {
            throw new InvalidOperationException($"Section '{sectionName}' is already registered to type {_sectionToType[sectionName].Name}");
        }

        _sectionToType[sectionName] = resourceType;
        _typeToSection[resourceType] = sectionName;
        
        var attribute = resourceType.GetCustomAttribute<ResourceTypeAttribute>();
        var info = new ResourceTypeInfo
        {
            Type = resourceType,
            SectionName = sectionName,
            Attribute = attribute,
            IsBaseResource = resourceType.IsSubclassOf(typeof(BaseResource))
        };
        
        _resourceTypes[sectionName] = info;
    }
}