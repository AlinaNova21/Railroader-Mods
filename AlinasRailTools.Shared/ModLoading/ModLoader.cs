using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AlinasRailTools.Shared.Definitions;
using Newtonsoft.Json;
using Serilog;

namespace AlinasRailTools.Shared.ModLoading;

/// <summary>
/// Handles discovering and loading Railloader mods from the game directory
/// Supports both physical mods (on disk) and virtual mods (in-memory)
/// </summary>
public class ModLoader
{
  private static readonly ILogger Logger = Log.ForContext<ModLoader>();
  private readonly string modsDirectory;
  private readonly List<VirtualModInfo> virtualMods = new();

  public ModLoader(string gameDirectory)
  {
    if (string.IsNullOrEmpty(gameDirectory))
      throw new ArgumentNullException(nameof(gameDirectory));

    modsDirectory = Path.Combine(gameDirectory, "Mods");
  }

  /// <summary>
  /// Registers a virtual mod that will be included in mod discovery
  /// Virtual mods allow ART to provide compatibility for other mods
  /// </summary>
  public void RegisterVirtualMod(VirtualModInfo virtualMod)
  {
    if (virtualMod == null)
      throw new ArgumentNullException(nameof(virtualMod));

    virtualMods.Add(virtualMod);
    Logger.Information("Registered virtual mod: {ModId} ({Name})",
      virtualMod.Definition.Id, virtualMod.Definition.Name);
  }

  /// <summary>
  /// Registers standard virtual mods for Railloader, StrangeCustoms and AlinasMapMod
  /// Call this when ART is replacing these mods
  /// </summary>
  public void RegisterStandardVirtualMods()
  {
    RegisterVirtualMod(VirtualModInfo.CreateRailloaderShim());
    RegisterVirtualMod(VirtualModInfo.CreateStrangeCustomsShim());
    RegisterVirtualMod(VirtualModInfo.CreateAlinasMapModShim());
  }

  /// <summary>
  /// Discovers all valid Railloader mods (physical and virtual)
  /// Virtual mods are yielded first, then physical mods from the Mods directory
  /// </summary>
  public IEnumerable<RLModInfo> DiscoverMods()
  {
    // Yield virtual mods first
    foreach (var virtualMod in virtualMods)
    {
      Logger.Debug("Including virtual mod: {ModId}", virtualMod.Definition.Id);
      yield return virtualMod;
    }

    // Then discover physical mods
    if (!Directory.Exists(modsDirectory))
    {
      yield break;
    }

    foreach (var modDirectory in Directory.GetDirectories(modsDirectory))
    {
      var definitionPath = Path.Combine(modDirectory, "Definition.json");

      if (!File.Exists(definitionPath))
        continue;

      RLModInfo modInfo = null;
      try
      {
        modInfo = LoadMod(modDirectory);
      }
      catch (Exception ex)
      {
        // Log error but continue discovering other mods
        Logger.Error(ex, "Error loading mod from {ModDirectory}", modDirectory);
        continue;
      }

      if (modInfo != null)
      {
        // Skip physical mods that conflict with virtual mods
        if (IsConflictingWithVirtualMod(modInfo))
        {
          Logger.Warning("Skipping physical mod {ModId} - conflicts with registered virtual mod",
            modInfo.Definition.Id);
          continue;
        }

        yield return modInfo;
      }
    }
  }

  /// <summary>
  /// Check if a physical mod conflicts with any registered virtual mod
  /// </summary>
  private bool IsConflictingWithVirtualMod(RLModInfo mod)
  {
    foreach (var virtualMod in virtualMods)
    {
      // Check if the physical mod has the same ID as a virtual mod
      if (mod.Definition.Id == virtualMod.Definition.Id)
      {
        return true;
      }

      // Check if virtual mod explicitly conflicts with this physical mod
      foreach (var conflict in virtualMod.Definition.ConflictsWith)
      {
        if (conflict.Id == mod.Definition.Id)
        {
          return true;
        }
      }
    }

    return false;
  }

  /// <summary>
  /// Loads a specific mod from a directory
  /// </summary>
  public RLModInfo LoadMod(string modDirectory)
  {
    var definitionPath = Path.Combine(modDirectory, "Definition.json");

    if (!File.Exists(definitionPath))
      throw new FileNotFoundException($"Definition.json not found in {modDirectory}");

    var json = File.ReadAllText(definitionPath);
    var definition = JsonConvert.DeserializeObject<RLModDefinition>(json);

    if (definition == null)
      throw new InvalidOperationException($"Failed to deserialize Definition.json from {modDirectory}");

    return new RLModInfo
    {
      Directory = modDirectory,
      Definition = definition
    };
  }

  /// <summary>
  /// Finds a mod by ID
  /// </summary>
  public RLModInfo FindMod(string modId)
  {
    return DiscoverMods().FirstOrDefault(m => m.Definition.Id == modId);
  }

  /// <summary>
  /// Discovers all mods and returns them in dependency order.
  /// Errors are logged in the result and via Serilog.
  /// </summary>
  public ModLoadOrderResult DiscoverModsInLoadOrder()
  {
    var mods = DiscoverMods().ToList();
    var sorter = new ModLoadOrderSorter();
    return sorter.Sort(mods);
  }
}
