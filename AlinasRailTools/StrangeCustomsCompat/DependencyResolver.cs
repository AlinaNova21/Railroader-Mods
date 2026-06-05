using System.Collections.Generic;
using UnityEngine;

namespace AlinasRailTools.StrangeCustomsCompat;

/// <summary>
/// Resolves mod dependencies and determines load order using topological sort
/// </summary>
public class DependencyResolver
{
    private readonly Dictionary<string, ModDefinition> mods;

    public DependencyResolver(Dictionary<string, ModDefinition> mods)
    {
        this.mods = mods;
    }

    /// <summary>
    /// Resolve dependencies and return mods in load order (dependencies first)
    /// </summary>
    public List<string> ResolveLoadOrder()
    {
        Debug.Log("DependencyResolver: Resolving dependencies...");

        var loadOrder = new List<string>();
        var visited = new HashSet<string>();
        var visiting = new HashSet<string>();

        foreach (var mod in mods.Values)
        {
            if (!visited.Contains(mod.Id))
            {
                ResolveDependenciesRecursive(mod.Id, visited, visiting, loadOrder);
            }
        }

        Debug.Log($"DependencyResolver: Load order determined: {string.Join(" -> ", loadOrder)}");
        return loadOrder;
    }

    /// <summary>
    /// Recursive depth-first search for topological sort
    /// </summary>
    private void ResolveDependenciesRecursive(
        string modId,
        HashSet<string> visited,
        HashSet<string> visiting,
        List<string> loadOrder)
    {
        // Detect circular dependencies
        if (visiting.Contains(modId))
        {
            Debug.LogError($"DependencyResolver: Circular dependency detected involving mod '{modId}'");
            return;
        }

        // Already processed
        if (visited.Contains(modId))
        {
            return;
        }

        // Handle missing dependencies (compatibility mode - just warn and continue)
        if (!mods.ContainsKey(modId))
        {
            Debug.LogWarning($"DependencyResolver: Missing dependency '{modId}' - ignoring (compatibility mode)");
            return;
        }

        visiting.Add(modId);
        var mod = mods[modId];

        // Process all dependencies first (depth-first)
        foreach (var depId in mod.Dependencies)
        {
            ResolveDependenciesRecursive(depId, visited, visiting, loadOrder);
        }

        visiting.Remove(modId);
        visited.Add(modId);
        loadOrder.Add(modId);
    }
}
