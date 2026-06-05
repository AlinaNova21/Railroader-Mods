using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AlinasRailTools.Shared.Resources;
using Serilog;

namespace AlinasRailTools.Resources;

/// <summary>
/// Registry for all resource managers, implements IResourceLocator for cross-manager lookups
/// </summary>
public class ManagerRegistry : IResourceLocator
{
  private static readonly ILogger Logger = Log.ForContext<ManagerRegistry>();

  private List<IResourceManager> _managers = new List<IResourceManager>();
  private List<IResourceManager> _sortedManagers;  // Cached sorted order
  private ManagerOrderResolver _orderResolver = new ManagerOrderResolver();

  private Dictionary<string, IResourceManager> _managersByType
    = new Dictionary<string, IResourceManager>();

  private Dictionary<Type, IResourceManager> _managersByEntityType
    = new Dictionary<Type, IResourceManager>();

  public void RegisterManager(IResourceManager manager)
  {
    _managers.Add(manager);
    _sortedManagers = null;  // Invalidate cache

    foreach (var typeName in manager.GetResourceTypes())
    {
      _managersByType[typeName] = manager;
      Logger.Information("Registered manager {Manager} for type {Type}",
        manager.GetType().Name, typeName);
    }

    // Track entity type -> manager mapping for BaseResourceManager<T>
    var managerType = manager.GetType();
    if (managerType.BaseType?.IsGenericType == true &&
        managerType.BaseType.GetGenericTypeDefinition() == typeof(BaseResourceManager<>))
    {
      var entityType = managerType.BaseType.GetGenericArguments()[0];
      _managersByEntityType[entityType] = manager;
    }
  }

  public IResourceManager GetManagerForType(string resourceType)
  {
    return _managersByType.TryGetValue(resourceType, out var mgr) ? mgr : null;
  }

  /// <summary>
  /// Get managers in dependency order
  /// </summary>
  private IEnumerable<IResourceManager> GetSortedManagers()
  {
    if (_sortedManagers == null)
    {
      _sortedManagers = _orderResolver.SortManagers(_managers);

      Logger.Information("Manager application order:");
      for (int i = 0; i < _sortedManagers.Count; i++)
      {
        var mgr = _sortedManagers[i];
        Logger.Information("  {Index}. {Manager} [{Types}]",
          i + 1,
          mgr.GetType().Name,
          string.Join(", ", mgr.GetResourceTypes()));
      }
    }

    return _sortedManagers;
  }

  /// <summary>
  /// Apply state to all managers IN DEPENDENCY ORDER
  /// </summary>
  public void ApplyToAll(StateFile state)
  {
    Logger.Information("ApplyToAll: Starting state application to {Count} managers", _managers.Count);

    var sortedManagers = GetSortedManagers().ToList();
    Logger.Information("ApplyToAll: Will apply in order: {Managers}",
      string.Join(" -> ", sortedManagers.Select(m => m.GetType().Name)));

    foreach (var manager in sortedManagers)
    {
      Logger.Information("ApplyToAll: Applying state to {Manager}", manager.GetType().Name);
      try
      {
        manager.ApplyFromState(state, this);
        Logger.Information("ApplyToAll: Successfully applied state to {Manager}", manager.GetType().Name);
      }
      catch (Exception ex)
      {
        Logger.Error(ex, "ApplyToAll: Error applying state to {Manager}", manager.GetType().Name);
      }
    }

    Logger.Information("ApplyToAll: Completed state application");
  }

  /// <summary>
  /// Snapshot all managers (order doesn't matter for snapshot)
  /// </summary>
  public StateFile SnapshotAll()
  {
    var state = new StateFile();

    foreach (var manager in _managers)
    {
      manager.SnapshotToState(state);
    }

    return state;
  }

  // IResourceLocator implementation

  public T GetResource<T>(ResId<T> id) where T : class
  {
    return GetResource<T>(id.Id);
  }

  public T GetResource<T>(string id) where T : class
  {
    // Find manager for this entity type
    if (!_managersByEntityType.TryGetValue(typeof(T), out var manager))
    {
      Logger.Warning("No manager found for entity type {Type}", typeof(T).Name);
      return null;
    }

    // Use reflection to call Get(id) on the manager
    var method = manager.GetType().GetMethod("Get", new[] { typeof(string) });
    return method?.Invoke(manager, new object[] { id }) as T;
  }

  public bool ResourceExists<T>(ResId<T> id)
  {
    if (!_managersByEntityType.TryGetValue(typeof(T), out var manager))
      return false;

    var method = manager.GetType().GetMethod("Exists", new[] { typeof(string) });
    return (bool)(method?.Invoke(manager, new object[] { id.Id }) ?? false);
  }

  public bool TryGetResource<T>(ResId<T> id, out T resource) where T : class
  {
    resource = GetResource<T>(id);
    return resource != null;
  }
}
