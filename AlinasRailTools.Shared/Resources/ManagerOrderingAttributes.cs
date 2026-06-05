using System;

namespace AlinasRailTools.Shared.Resources;

/// <summary>
/// Specifies that this manager should apply its resources AFTER
/// the specified resource types have been applied.
/// Can be specified multiple times for multiple dependencies.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ManagerAfterAttribute : Attribute
{
  public string[] ResourceTypes { get; }

  public ManagerAfterAttribute(params string[] resourceTypes)
  {
    ResourceTypes = resourceTypes;
  }
}

/// <summary>
/// Specifies that this manager should apply its resources BEFORE
/// the specified resource types are applied.
/// Can be specified multiple times for multiple dependents.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ManagerBeforeAttribute : Attribute
{
  public string[] ResourceTypes { get; }

  public ManagerBeforeAttribute(params string[] resourceTypes)
  {
    ResourceTypes = resourceTypes;
  }
}
