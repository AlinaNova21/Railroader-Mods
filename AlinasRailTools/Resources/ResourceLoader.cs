using System.Collections.Generic;
using System.IO;
using AlinasRailTools.Shared.Definitions;
using AlinasRailTools.Shared.ModLoading;
using AlinasRailTools.Shared.Resources;
using Newtonsoft.Json.Linq;
using Serilog;

namespace AlinasRailTools.Resources;

/// <summary>
/// Loads mods using snapshot-overlay-apply pattern
/// </summary>
public class ResourceLoader
{
  private static readonly Serilog.ILogger Logger = Log.ForContext<ResourceLoader>();

  private readonly ManagerRegistry _registry;
  private readonly StateMerger _merger;
  private readonly LegacyConverter _legacyConverter;

  public ResourceLoader(ManagerRegistry registry)
  {
    _registry = registry;
    _merger = new StateMerger();
    _legacyConverter = new LegacyConverter();
  }

  /// <summary>
  /// Load flow: Snapshot → Overlay Files → Apply Final Snapshot
  /// </summary>
  public void LoadModsSequentially(IEnumerable<RLModInfo> sortedMods)
  {
    // 1. Take initial snapshot of current state
    var currentSnapshot = _registry.SnapshotAll();

    Logger.Information("Starting mod loading with initial state snapshot");

    // 2. Overlay each mod's state files sequentially
    foreach (var mod in sortedMods)
    {
      Logger.Information("Loading mod: {ModId}", mod.Definition.Id);

      foreach (var stateFile in GetModStateFiles(mod))
      {
        currentSnapshot = _merger.Overlay(currentSnapshot, stateFile);
      }
    }

    // 3. Apply final merged snapshot to all managers (in dependency order)
    Logger.Information("Applying final merged state to managers");
    _registry.ApplyToAll(currentSnapshot);

    Logger.Information("Mod loading complete");
  }

  private IEnumerable<StateFile> GetModStateFiles(RLModInfo mod)
  {
    // Iterate through mixintos
    if (mod.Definition.Mixintos == null)
      yield break;

    foreach (var kvp in mod.Definition.Mixintos)
    {
      foreach (var reference in kvp.Value)
      {
        var filePath = Path.Combine(mod.Directory, reference.File);

        if (!File.Exists(filePath))
        {
          Logger.Warning("State file not found: {Path}", filePath);
          continue;
        }

        var json = File.ReadAllText(filePath);
        yield return LoadStateFile(json);
      }
    }
  }

  private StateFile LoadStateFile(string json)
  {
    var jobj = JObject.Parse(json);

    // Detect legacy format
    if (IsLegacyFormat(jobj))
    {
      Logger.Debug("Converting legacy game-graph format");
      return _legacyConverter.ConvertToStateFile(jobj);
    }

    // Parse new format
    return StateFile.FromJson(json);
  }

  private bool IsLegacyFormat(JObject obj)
  {
    // Check for legacy keys
    return obj["tracks"] != null || obj["progressions"] != null;
  }
}
