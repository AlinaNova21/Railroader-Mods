using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;

namespace AlinasRailTools.Shared.ModLoading;

/// <summary>
/// Types of mod loading errors
/// </summary>
public enum ModLoadErrorType
{
  CircularDependency,
  MissingDependency,
  Conflict
}

/// <summary>
/// Represents an error encountered during mod loading
/// </summary>
public class ModLoadError
{
  public ModLoadErrorType Type { get; set; }
  public string ModId { get; set; }
  public string RelatedModId { get; set; }
  public string Message { get; set; }
  public List<string> Cycle { get; set; }

  public static ModLoadError CircularDependency(List<string> cycle)
  {
    return new ModLoadError
    {
      Type = ModLoadErrorType.CircularDependency,
      Cycle = cycle,
      Message = $"Circular dependency detected: {string.Join(" -> ", cycle)}"
    };
  }

  public static ModLoadError MissingDependency(string modId, string requiredModId)
  {
    return new ModLoadError
    {
      Type = ModLoadErrorType.MissingDependency,
      ModId = modId,
      RelatedModId = requiredModId,
      Message = $"Mod '{modId}' requires '{requiredModId}' which is not available"
    };
  }

  public static ModLoadError Conflict(string modId, string conflictingModId)
  {
    return new ModLoadError
    {
      Type = ModLoadErrorType.Conflict,
      ModId = modId,
      RelatedModId = conflictingModId,
      Message = $"Mod '{modId}' conflicts with '{conflictingModId}'"
    };
  }
}

/// <summary>
/// Result of mod load order sorting
/// </summary>
public class ModLoadOrderResult
{
  public List<RLModInfo> SortedMods { get; set; } = new List<RLModInfo>();
  public List<RLModInfo> SkippedMods { get; set; } = new List<RLModInfo>();
  public List<ModLoadError> Errors { get; set; } = new List<ModLoadError>();

  public bool HasErrors => Errors.Count > 0;
}

/// <summary>
/// Performs topological sorting of mods based on their dependencies
/// </summary>
public class ModLoadOrderSorter
{
  private static readonly ILogger Logger = Log.ForContext<ModLoadOrderSorter>();

  /// <summary>
  /// Sorts mods in dependency order, respecting requires, loadBefore, and loadAfter constraints.
  /// Errors are collected and returned in the result, and logged via Serilog.
  /// </summary>
  /// <param name="mods">The mods to sort</param>
  /// <returns>Result containing sorted mods, skipped mods, and any errors encountered</returns>
  public ModLoadOrderResult Sort(IEnumerable<RLModInfo> mods)
  {
    var result = new ModLoadOrderResult();
    var modList = mods.ToList();

    // Deduplicate by mod ID (take first occurrence)
    var modLookup = new Dictionary<string, RLModInfo>();
    foreach (var mod in modList)
    {
      if (!modLookup.ContainsKey(mod.Definition.Id))
      {
        modLookup[mod.Definition.Id] = mod;
      }
      else
      {
        Logger.Warning("Duplicate mod ID '{ModId}' encountered - using first occurrence from {Directory}",
          mod.Definition.Id, modLookup[mod.Definition.Id].Directory);
      }
    }

    var modsToSkip = new HashSet<string>();

    // Check for conflicts first
    CheckConflicts(modList, modLookup, result, modsToSkip);

    // Build dependency graph, tracking missing dependencies
    var graph = BuildDependencyGraph(modList, modLookup, result, modsToSkip);

    // Remove skipped mods from processing
    var modsToSort = modList.Where(m => !modsToSkip.Contains(m.Definition.Id)).ToList();
    result.SkippedMods.AddRange(modList.Where(m => modsToSkip.Contains(m.Definition.Id)));

    // Perform topological sort
    if (modsToSort.Count > 0)
    {
      TopologicalSort(modsToSort, graph, result);
    }

    return result;
  }

  private void CheckConflicts(
    List<RLModInfo> mods,
    Dictionary<string, RLModInfo> modLookup,
    ModLoadOrderResult result,
    HashSet<string> modsToSkip)
  {
    foreach (var mod in mods)
    {
      foreach (var conflict in mod.Definition.ConflictsWith)
      {
        if (modLookup.ContainsKey(conflict.Id))
        {
          var error = ModLoadError.Conflict(mod.Definition.Id, conflict.Id);
          result.Errors.Add(error);
          Logger.Error("Mod conflict: {ModId} conflicts with {ConflictingModId}",
            mod.Definition.Id, conflict.Id);
          modsToSkip.Add(mod.Definition.Id);
          break; // Skip this mod entirely
        }
      }
    }
  }

  private Dictionary<string, HashSet<string>> BuildDependencyGraph(
    List<RLModInfo> mods,
    Dictionary<string, RLModInfo> modLookup,
    ModLoadOrderResult result,
    HashSet<string> modsToSkip)
  {
    var graph = new Dictionary<string, HashSet<string>>();

    // Initialize graph nodes
    foreach (var mod in mods)
    {
      graph[mod.Definition.Id] = new HashSet<string>();
    }

    // Build edges based on dependencies
    foreach (var mod in mods)
    {
      var modId = mod.Definition.Id;

      // Skip mods that have conflicts
      if (modsToSkip.Contains(modId))
        continue;

      // requires: this mod needs the dependency to be loaded first
      // So dependency -> this mod
      foreach (var requirement in mod.Definition.Requires)
      {
        if (!modLookup.ContainsKey(requirement.Id))
        {
          var error = ModLoadError.MissingDependency(modId, requirement.Id);
          result.Errors.Add(error);
          Logger.Error("Missing dependency: {ModId} requires {RequiredModId} which is not available",
            modId, requirement.Id);
          modsToSkip.Add(modId);
          break; // Skip this mod entirely
        }

        // Don't add edges to skipped mods
        if (!modsToSkip.Contains(requirement.Id))
        {
          // requirement must load before this mod
          graph[requirement.Id].Add(modId);
        }
      }

      // Skip rest of processing if mod is being skipped
      if (modsToSkip.Contains(modId))
        continue;

      // loadAfter: this mod should load after the specified mods
      // So dependency -> this mod
      foreach (var loadAfter in mod.Definition.LoadAfter)
      {
        if (!modLookup.ContainsKey(loadAfter.Id))
        {
          // loadAfter is optional, just skip if not present
          continue;
        }

        // Don't add edges to skipped mods
        if (!modsToSkip.Contains(loadAfter.Id))
        {
          // loadAfter mod must load before this mod
          graph[loadAfter.Id].Add(modId);
        }
      }

      // loadBefore: this mod should load before the specified mods
      // So this mod -> dependency
      foreach (var loadBefore in mod.Definition.LoadBefore)
      {
        if (!modLookup.ContainsKey(loadBefore.Id))
        {
          // loadBefore is optional, just skip if not present
          continue;
        }

        // Don't add edges to skipped mods
        if (!modsToSkip.Contains(loadBefore.Id))
        {
          // this mod must load before the loadBefore mod
          graph[modId].Add(loadBefore.Id);
        }
      }
    }

    return graph;
  }

  private void TopologicalSort(
    List<RLModInfo> modsToSort,
    Dictionary<string, HashSet<string>> graph,
    ModLoadOrderResult result)
  {
    var modLookup = modsToSort.ToDictionary(m => m.Definition.Id, m => m);
    var visited = new HashSet<string>();
    var visiting = new HashSet<string>();
    var visitPath = new List<string>();

    foreach (var mod in modsToSort)
    {
      var modId = mod.Definition.Id;
      if (!visited.Contains(modId))
      {
        Visit(modId, graph, visited, visiting, visitPath, result, modLookup);
      }
    }
  }

  private void Visit(
    string modId,
    Dictionary<string, HashSet<string>> graph,
    HashSet<string> visited,
    HashSet<string> visiting,
    List<string> visitPath,
    ModLoadOrderResult result,
    Dictionary<string, RLModInfo> modLookup)
  {
    if (visiting.Contains(modId))
    {
      // Circular dependency detected - log error and skip this branch
      var cycleStart = visitPath.IndexOf(modId);
      var cycle = visitPath.Skip(cycleStart).Append(modId).ToList();
      var error = ModLoadError.CircularDependency(cycle);
      result.Errors.Add(error);
      Logger.Error("Circular dependency detected: {Cycle}", string.Join(" -> ", cycle));

      // Mark all mods in cycle as skipped
      foreach (var cycleModId in cycle)
      {
        if (modLookup.ContainsKey(cycleModId))
        {
          var cycledMod = modLookup[cycleModId];
          if (!result.SkippedMods.Contains(cycledMod))
          {
            result.SkippedMods.Add(cycledMod);
          }
        }
      }
      return;
    }

    if (visited.Contains(modId))
    {
      return;
    }

    visiting.Add(modId);
    visitPath.Add(modId);

    // Visit all dependencies first
    if (graph.ContainsKey(modId))
    {
      foreach (var dependency in graph[modId])
      {
        Visit(dependency, graph, visited, visiting, visitPath, result, modLookup);
      }
    }

    visiting.Remove(modId);
    visitPath.RemoveAt(visitPath.Count - 1);
    visited.Add(modId);

    // Add to result after all dependencies are processed
    // Insert at beginning so dependencies come before dependents
    // Only add if not already in skipped list (due to circular dependency)
    if (modLookup.ContainsKey(modId) && !result.SkippedMods.Contains(modLookup[modId]))
    {
      result.SortedMods.Insert(0, modLookup[modId]);
    }
  }
}
