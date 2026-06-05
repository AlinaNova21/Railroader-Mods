namespace AlinasRailTools.Shared.Resources;

/// <summary>
/// Provides cross-manager resource lookups during state application
/// </summary>
public interface IResourceLocator
{
  /// <summary>
  /// Get resource by ID with type inference from ResId
  /// </summary>
  T GetResource<T>(ResId<T> id) where T : class;

  /// <summary>
  /// Get resource by string ID
  /// </summary>
  T GetResource<T>(string id) where T : class;

  /// <summary>
  /// Check if resource exists
  /// </summary>
  bool ResourceExists<T>(ResId<T> id);

  /// <summary>
  /// Try get resource (returns null if not found)
  /// </summary>
  bool TryGetResource<T>(ResId<T> id, out T resource) where T : class;
}
