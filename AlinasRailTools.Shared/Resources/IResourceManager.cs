namespace AlinasRailTools.Shared.Resources;

/// <summary>
/// Interface for resource managers that handle loading and snapshotting resources
/// </summary>
public interface IResourceManager
{
  /// <summary>
  /// Get resource types this manager handles
  /// </summary>
  string[] GetResourceTypes();

  /// <summary>
  /// Pull resources from state file and apply them.
  /// Manager extracts only the resource types it handles.
  /// </summary>
  void ApplyFromState(StateFile state, IResourceLocator locator);

  /// <summary>
  /// Populate state file with current snapshot.
  /// Manager adds its resource types to the state.
  /// </summary>
  void SnapshotToState(StateFile state);
}
