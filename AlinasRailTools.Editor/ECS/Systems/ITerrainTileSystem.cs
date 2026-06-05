using System.Numerics;

namespace AlinasRailTools.Editor.ECS.Systems;

/// <summary>
/// Interface for terrain tile system
/// Manages terrain tile entities based on camera position
/// </summary>
public interface ITerrainTileSystem : ISystem
{
    /// <summary>
    /// Update terrain tiles based on camera position
    /// Spawns/despawns tiles as needed
    /// </summary>
    /// <param name="cameraPosition">Current camera target position</param>
    void Update(Vector3 cameraPosition);

    /// <summary>
    /// Process pending heightmap loads on the main thread
    /// Must be called from render/main thread
    /// </summary>
    void ProcessPendingLoads();

    /// <summary>
    /// Get count of currently active tiles
    /// </summary>
    int GetActiveTileCount();

    /// <summary>
    /// Get count of loaded tiles (heightmap ready)
    /// </summary>
    int GetLoadedTileCount();

    /// <summary>
    /// Get terrain height at world position by sampling heightmap
    /// Returns base height (500m) if tile not loaded
    /// </summary>
    float GetTerrainHeightAt(Vector3 worldPos);
}
