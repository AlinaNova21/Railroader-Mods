using System;

namespace AlinasRailTools.Shared.Resources;

/// <summary>
/// Marks a class as handling specific resource types (camelCase names)
/// Used for both manager discovery and serialized type registration
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ResourceTypeAttribute : Attribute
{
  public string[] TypeName { get; }

  public ResourceTypeAttribute(params string[] typeName)
  {
    TypeName = typeName;
  }
}
