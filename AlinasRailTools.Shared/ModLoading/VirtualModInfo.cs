using AlinasRailTools.Shared.Definitions;

namespace AlinasRailTools.Shared.ModLoading;

/// <summary>
/// Represents a virtual mod that exists in-memory without a physical directory.
/// Used for compatibility shims where ART replaces functionality from other mods.
/// </summary>
public class VirtualModInfo : RLModInfo
{
  /// <summary>
  /// Indicates this is a virtual mod with no physical files
  /// </summary>
  public bool IsVirtual => true;

  /// <summary>
  /// Create a virtual mod with a synthetic definition
  /// </summary>
  public static VirtualModInfo Create(RLModDefinition definition)
  {
    return new VirtualModInfo
    {
      Directory = null, // No physical directory
      Definition = definition
    };
  }

  /// <summary>
  /// Create a virtual mod for Railloader compatibility
  /// This satisfies dependencies on railloader
  /// </summary>
  public static VirtualModInfo CreateRailloaderShim()
  {
    return Create(new RLModDefinition
    {
      Id = "railloader",
      Name = "Railloader (Virtual)",
      Version = "99.99.99", // Very high version to satisfy all notBefore checks
      Requires = new RLModRequirement[0],
      LoadAfter = new RLModRequirement[0],
      LoadBefore = new RLModRequirement[0],
      ConflictsWith = new RLModRequirement[0]
    });
  }

  /// <summary>
  /// Create a virtual mod for StrangeCustoms compatibility
  /// This allows mods that depend on StrangeCustoms to work with ART
  /// </summary>
  public static VirtualModInfo CreateStrangeCustomsShim()
  {
    return Create(new RLModDefinition
    {
      Id = "Zamu.StrangeCustoms",
      Name = "Strange Customs (Virtual)",
      Version = "99.99.99", // Very high version to satisfy all notBefore checks
      Requires = new RLModRequirement[0],
      LoadAfter = new RLModRequirement[0],
      LoadBefore = new RLModRequirement[0],
      ConflictsWith = new RLModRequirement[0]
    });
  }

  /// <summary>
  /// Create a virtual mod for AlinasMapMod compatibility
  /// This allows mods that depend on AlinasMapMod to work with ART
  /// </summary>
  public static VirtualModInfo CreateAlinasMapModShim()
  {
    return Create(new RLModDefinition
    {
      Id = "AlinaNova21.AlinasMapMod",
      Name = "Alinas Map Mod (Virtual)",
      Version = "99.99.99", // Very high version to satisfy all notBefore checks
      Requires = new RLModRequirement[0],
      LoadAfter = new RLModRequirement[0],
      LoadBefore = new RLModRequirement[0],
      ConflictsWith = new RLModRequirement[0]
    });
  }
}
