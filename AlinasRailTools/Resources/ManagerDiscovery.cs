using System.Linq;
using AlinasRailTools.Shared.Resources;
using Serilog;
using UnityEngine;

namespace AlinasRailTools.Resources;

/// <summary>
/// Discovers and registers resource managers in the scene
/// </summary>
public static class ManagerDiscovery
{
  private static readonly Serilog.ILogger Logger = Log.ForContext(typeof(ManagerDiscovery));

  /// <summary>
  /// Discover all IResourceManager instances in scene and register them
  /// </summary>
  public static void DiscoverAndRegister(ManagerRegistry registry)
  {
    var managers = Object.FindObjectsOfType<MonoBehaviour>()
      .OfType<IResourceManager>()
      .ToList();

    Logger.Information("Discovered {Count} resource managers", managers.Count);

    foreach (var manager in managers)
    {
      registry.RegisterManager(manager);
    }
  }
}
