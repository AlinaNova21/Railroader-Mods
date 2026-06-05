using System;

namespace AlinasRailTools.Core.Attributes;

/// <summary>
/// Marks a resource class with its section name for automatic discovery and reference validation
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ResourceTypeAttribute : Attribute
{
    /// <summary>
    /// The section name this resource type belongs to (e.g., "trackNodes", "trackSegments", "maps")
    /// </summary>
    public string SectionName { get; }

    /// <summary>
    /// Create a resource type attribute
    /// </summary>
    /// <param name="sectionName">The section name for this resource type</param>
    public ResourceTypeAttribute(string sectionName)
    {
        SectionName = sectionName ?? throw new ArgumentNullException(nameof(sectionName));
    }
}