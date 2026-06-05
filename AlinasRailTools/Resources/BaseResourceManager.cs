using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AlinasRailTools.Shared.Resources;
using Newtonsoft.Json.Linq;
using Serilog;
using UnityEngine;

namespace AlinasRailTools.Resources;

/// <summary>
/// Base class for resource managers. Handles:
/// - Object lifecycle (create/update/delete as children)
/// - Resource type discovery via attribute
/// - Snapshot/apply operations
/// </summary>
public abstract class BaseResourceManager<TEntity> : MonoBehaviour, IResourceManager
  where TEntity : Component
{
  private static readonly Serilog.ILogger Logger = Log.ForContext<BaseResourceManager<TEntity>>();

  // Track all managed entities
  protected Dictionary<string, TEntity> Entities = new Dictionary<string, TEntity>();

  // Resource type name (from attribute)
  private string _resourceTypeName;

  protected virtual void Awake()
  {
    // Discover resource type from attribute
    var attr = GetType().GetCustomAttribute<ResourceTypeAttribute>();
    if (attr == null || attr.TypeName == null || attr.TypeName.Length == 0)
    {
      throw new InvalidOperationException(
        $"{GetType().Name} must have [ResourceType] attribute");
    }

    _resourceTypeName = attr.TypeName[0]; // First type is primary
  }

  public virtual string[] GetResourceTypes()
  {
    var attr = GetType().GetCustomAttribute<ResourceTypeAttribute>();
    return attr?.TypeName ?? Array.Empty<string>();
  }

  public virtual void ApplyFromState(StateFile state, IResourceLocator locator)
  {
    var resources = state.GetResources(_resourceTypeName);
    Logger.Information("ApplyFromState [{Type}]: Found {Count} resources in state",
      _resourceTypeName, resources.Count);

    var processedIds = new HashSet<string>();

    foreach (var kvp in resources)
    {
      if (kvp.Value == null)
      {
        // Null = deletion marker
        Logger.Debug("ApplyFromState [{Type}]: Removing {Id}", _resourceTypeName, kvp.Key);
        Remove(kvp.Key);
      }
      else
      {
        // Apply (create or update)
        Logger.Debug("ApplyFromState [{Type}]: Applying {Id}", _resourceTypeName, kvp.Key);
        try
        {
          Apply(kvp.Key, kvp.Value, locator);
          processedIds.Add(kvp.Key);
        }
        catch (Exception ex)
        {
          Logger.Error(ex, "ApplyFromState [{Type}]: Error applying {Id}", _resourceTypeName, kvp.Key);
        }
      }
    }

    // Remove entities not in snapshot
    var toRemove = Entities.Keys.Except(processedIds).ToList();
    if (toRemove.Count > 0)
    {
      Logger.Information("ApplyFromState [{Type}]: Removing {Count} entities not in state: {Ids}",
        _resourceTypeName, toRemove.Count, string.Join(", ", toRemove));
    }
    foreach (var id in toRemove)
    {
      Remove(id);
    }

    Logger.Information("ApplyFromState [{Type}]: Completed. Now have {Count} entities",
      _resourceTypeName, Entities.Count);
  }

  public virtual void SnapshotToState(StateFile state)
  {
    var snapshot = new Dictionary<string, JObject>();

    foreach (var kvp in Entities)
    {
      var serialized = Serialize(kvp.Key, kvp.Value);
      if (serialized != null)
      {
        snapshot[kvp.Key] = JObject.FromObject(serialized);
      }
    }

    state.SetResources(_resourceTypeName, snapshot);
  }

  /// <summary>
  /// Get an entity by ID
  /// </summary>
  public TEntity Get(string id)
  {
    return Entities.TryGetValue(id, out var entity) ? entity : null;
  }

  /// <summary>
  /// Get entity with strongly-typed ID
  /// </summary>
  public TEntity Get(ResId<TEntity> id)
  {
    return Get(id.Id);
  }

  /// <summary>
  /// Check if entity exists
  /// </summary>
  public bool Exists(string id)
  {
    return Entities.ContainsKey(id);
  }

  /// <summary>
  /// List all entities
  /// </summary>
  public IReadOnlyList<TEntity> List()
  {
    return Entities.Values.ToList();
  }

  /// <summary>
  /// Apply a resource (create or update)
  /// Override to customize behavior
  /// </summary>
  protected virtual TEntity Apply(string id, JObject data, IResourceLocator locator)
  {
    if (!Entities.TryGetValue(id, out var entity))
    {
      // Create new entity as child
      entity = CreateEntity(id);
      Entities[id] = entity;
    }

    // Update entity from data
    UpdateEntity(id, entity, data, locator);

    return entity;
  }

  /// <summary>
  /// Remove an entity
  /// Override to customize behavior
  /// </summary>
  protected virtual void Remove(string id)
  {
    if (Entities.TryGetValue(id, out var entity))
    {
      Entities.Remove(id);
      if (entity != null)
      {
        Destroy(entity.gameObject);
      }
    }
  }

  /// <summary>
  /// Create a new entity GameObject as child of this manager
  /// Override to customize GameObject setup
  /// </summary>
  protected virtual TEntity CreateEntity(string id)
  {
    var go = new GameObject($"{typeof(TEntity).Name}_{id}");
    go.transform.SetParent(transform);
    return go.AddComponent<TEntity>();
  }

  /// <summary>
  /// Update entity from data
  /// MUST be implemented by derived classes
  /// </summary>
  protected abstract void UpdateEntity(
    string id,
    TEntity entity,
    JObject data,
    IResourceLocator locator);

  /// <summary>
  /// Serialize entity to data
  /// MUST be implemented by derived classes
  /// </summary>
  protected abstract object Serialize(string id, TEntity entity);
}
