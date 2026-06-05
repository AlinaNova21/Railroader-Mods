using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AlinasRailTools.Shared.Resources;
using Serilog;

namespace AlinasRailTools.Resources;

/// <summary>
/// Resolves manager application order based on [ManagerAfter] and [ManagerBefore] dependencies.
/// Uses topological sorting similar to ModLoadOrderSorter.
/// </summary>
public class ManagerOrderResolver
{
  private static readonly ILogger Logger = Log.ForContext<ManagerOrderResolver>();

  public class OrderedManager
  {
    public IResourceManager Manager { get; set; }
    public string[] ResourceTypes { get; set; }
    public string[] RunsAfter { get; set; }   // From [ManagerAfter]
    public string[] RunsBefore { get; set; }  // From [ManagerBefore]
  }

  /// <summary>
  /// Sort managers by dependency order, handling both After and Before constraints
  /// </summary>
  public List<IResourceManager> SortManagers(IEnumerable<IResourceManager> managers)
  {
    var managerInfos = managers.Select(m => new OrderedManager
    {
      Manager = m,
      ResourceTypes = m.GetResourceTypes(),
      RunsAfter = GetManagerAfterDependencies(m),
      RunsBefore = GetManagerBeforeDependencies(m)
    }).ToList();

    // Build lookup: resourceType -> manager
    var typeToManager = new Dictionary<string, OrderedManager>();
    foreach (var info in managerInfos)
    {
      foreach (var type in info.ResourceTypes)
      {
        typeToManager[type] = info;
      }
    }

    // Build dependency graph: manager -> dependencies that must run before it
    var graph = new Dictionary<OrderedManager, HashSet<OrderedManager>>();
    foreach (var info in managerInfos)
    {
      graph[info] = new HashSet<OrderedManager>();
    }

    // Process [ManagerAfter]: this manager depends on specified types
    foreach (var info in managerInfos)
    {
      foreach (var depType in info.RunsAfter)
      {
        if (typeToManager.TryGetValue(depType, out var depManager))
        {
          // This manager must run after depManager
          graph[info].Add(depManager);
        }
        else
        {
          Logger.Warning(
            "Manager {Manager} declares ManagerAfter({Type}) but no manager provides that type",
            info.Manager.GetType().Name,
            depType);
        }
      }
    }

    // Process [ManagerBefore]: specified types depend on this manager
    foreach (var info in managerInfos)
    {
      foreach (var dependentType in info.RunsBefore)
      {
        if (typeToManager.TryGetValue(dependentType, out var dependentManager))
        {
          // dependentManager must run after this manager
          graph[dependentManager].Add(info);
        }
        else
        {
          Logger.Warning(
            "Manager {Manager} declares ManagerBefore({Type}) but no manager provides that type",
            info.Manager.GetType().Name,
            dependentType);
        }
      }
    }

    // Topological sort
    var sorted = new List<IResourceManager>();
    var visited = new HashSet<OrderedManager>();
    var visiting = new HashSet<OrderedManager>();
    var visitPath = new List<string>();

    foreach (var manager in managerInfos)
    {
      if (!visited.Contains(manager))
      {
        Visit(manager, graph, visited, visiting, visitPath, sorted);
      }
    }

    return sorted;
  }

  private void Visit(
    OrderedManager manager,
    Dictionary<OrderedManager, HashSet<OrderedManager>> graph,
    HashSet<OrderedManager> visited,
    HashSet<OrderedManager> visiting,
    List<string> visitPath,
    List<IResourceManager> sorted)
  {
    var managerName = manager.Manager.GetType().Name;

    if (visiting.Contains(manager))
    {
      // Circular dependency detected
      visitPath.Add(managerName);
      var cycle = string.Join(" -> ", visitPath);
      Logger.Error("Circular manager dependency detected: {Cycle}", cycle);

      // Continue but don't add to sorted (will be skipped)
      return;
    }

    if (visited.Contains(manager))
      return;

    visiting.Add(manager);
    visitPath.Add(managerName);

    // Visit all dependencies first (managers that must run before this one)
    foreach (var dep in graph[manager])
    {
      Visit(dep, graph, visited, visiting, visitPath, sorted);
    }

    visiting.Remove(manager);
    visitPath.RemoveAt(visitPath.Count - 1);
    visited.Add(manager);

    // Add to sorted list (dependencies already added)
    sorted.Add(manager.Manager);
  }

  private string[] GetManagerAfterDependencies(IResourceManager manager)
  {
    var managerType = manager.GetType();
    var afterAttrs = managerType.GetCustomAttributes<ManagerAfterAttribute>();

    return afterAttrs.SelectMany(attr => attr.ResourceTypes).ToArray();
  }

  private string[] GetManagerBeforeDependencies(IResourceManager manager)
  {
    var managerType = manager.GetType();
    var beforeAttrs = managerType.GetCustomAttributes<ManagerBeforeAttribute>();

    return beforeAttrs.SelectMany(attr => attr.ResourceTypes).ToArray();
  }
}
