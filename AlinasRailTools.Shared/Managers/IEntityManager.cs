using System.Collections.Generic;

namespace AlinasRailTools.Shared.Managers;

/// <summary>
/// Generic CRUD interface for managing game entities
/// </summary>
/// <typeparam name="TEntity">The Unity/game entity type</typeparam>
/// <typeparam name="TSerialized">The serialized definition type</typeparam>
public interface IEntityManager<TEntity, TSerialized>
{
  /// <summary>
  /// Creates or updates an entity from serialized data
  /// </summary>
  TEntity Apply(string id, TSerialized data);

  /// <summary>
  /// Removes an entity by ID
  /// </summary>
  void Remove(string id);

  /// <summary>
  /// Gets an entity by ID
  /// </summary>
  TEntity Get(string id);

  /// <summary>
  /// Serializes an entity to its definition format
  /// </summary>
  TSerialized Serialize(string id);

  /// <summary>
  /// Lists all active entities
  /// </summary>
  List<TEntity> List();

  /// <summary>
  /// Checks if an entity exists
  /// </summary>
  bool Exists(string id);
}

/// <summary>
/// Read-only interface for querying entities
/// </summary>
public interface IReadOnlyEntityManager<TEntity>
{
  TEntity Get(string id);
  List<TEntity> List();
  bool Exists(string id);
}
