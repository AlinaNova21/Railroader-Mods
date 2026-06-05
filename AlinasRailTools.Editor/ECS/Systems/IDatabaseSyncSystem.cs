using Arch.Core;

namespace AlinasRailTools.Editor.ECS.Systems;

/// <summary>
/// Interface for database synchronization system
/// Syncs ECS entities with the Working State Database
/// </summary>
public interface IDatabaseSyncSystem : IUpdateSystem
{
    /// <summary>
    /// Get entity for a track node ID
    /// </summary>
    /// <param name="nodeId">Node identifier</param>
    /// <returns>Entity or null if not found</returns>
    Entity? GetNodeEntity(string nodeId);

    /// <summary>
    /// Get entity for a track segment ID
    /// </summary>
    /// <param name="segmentId">Segment identifier</param>
    /// <returns>Entity or null if not found</returns>
    Entity? GetSegmentEntity(string segmentId);
}
