using System.Numerics;
using Arch.Core;
using Arch.Core.Extensions;
using Serilog;
using Veldrid;
using AlinasRailTools.Editor.ECS.Components;
using AlinasRailTools.Editor.Rendering;

namespace AlinasRailTools.Editor.ECS.Systems;

/// <summary>
/// System that manages terrain tile entities based on camera position
/// Spawns/despawns tiles and triggers async heightmap loading
/// </summary>
public class TerrainTileSystem : ITerrainTileSystem
{
    private readonly ILogger _logger;
    private readonly World _world;
    private readonly VeldridTerrainManager _terrainManager;
    private readonly GraphicsDevice _device;
    private readonly Dictionary<Vector2Int, Entity> _activeTiles;
    private readonly int _loadRadius;

    public TerrainTileSystem(World world, VeldridTerrainManager terrainManager, GraphicsDevice device, int loadRadius = 100)
    {
        _logger = Log.ForContext<TerrainTileSystem>();
        _world = world;
        _terrainManager = terrainManager;
        _device = device;
        _activeTiles = new Dictionary<Vector2Int, Entity>();
        _loadRadius = loadRadius;

        _logger.Information("Initialized with load radius: {LoadRadius}", loadRadius);
    }

    /// <summary>
    /// Update tile entities based on camera position
    /// </summary>
    public void Update(Vector3 cameraPosition)
    {
        // Calculate current camera tile
        // Note: Negate Z because Veldrid camera position has -Z, but tile coordinates use +Y for south
        int cameraTileX = (int)Math.Floor(cameraPosition.X / VeldridTerrainManager.TileDimension);
        int cameraTileZ = (int)Math.Floor(-cameraPosition.Z / VeldridTerrainManager.TileDimension);

        // Determine which tiles should be loaded (circular pattern, not square)
        HashSet<Vector2Int> requiredTiles = new HashSet<Vector2Int>();
        for (int x = cameraTileX - _loadRadius; x <= cameraTileX + _loadRadius; x++)
        {
            for (int z = cameraTileZ - _loadRadius; z <= cameraTileZ + _loadRadius; z++)
            {
                // Calculate distance from camera tile (squared to avoid sqrt)
                int dx = x - cameraTileX;
                int dz = z - cameraTileZ;
                int distanceSquared = dx * dx + dz * dz;

                // Only load if within circular radius
                if (distanceSquared <= _loadRadius * _loadRadius)
                {
                    requiredTiles.Add(new Vector2Int(x, z));
                }
            }
        }

        // Remove tiles that are no longer needed
        var tilesToRemove = new List<Vector2Int>();
        foreach (var (tileCoord, entity) in _activeTiles)
        {
            if (!requiredTiles.Contains(tileCoord))
            {
                tilesToRemove.Add(tileCoord);

                // Dispose GPU resources BEFORE destroying entity
                if (_world.IsAlive(entity) && entity.Has<TerrainTileComponent>())
                {
                    ref var tile = ref entity.Get<TerrainTileComponent>();
                    tile.HeightTextureResourceSet?.Dispose();
                    tile.HeightTextureView?.Dispose();
                    tile.HeightTexture?.Dispose();
                    tile.HeightmapData = null; // Allow GC to collect
                }

                _world.Destroy(entity);
            }
        }
        foreach (var coord in tilesToRemove)
        {
            _activeTiles.Remove(coord);
        }

        // Add new tiles that are now in range
        foreach (var tileCoord in requiredTiles)
        {
            if (!_activeTiles.ContainsKey(tileCoord))
            {
                // Create new tile entity
                var entity = _world.Create(
                    new TerrainTileComponent(tileCoord.X, tileCoord.Y)
                );
                _activeTiles[tileCoord] = entity;

                // Trigger async heightmap loading via terrain manager
                _terrainManager.LoadTileAsync(tileCoord.X, tileCoord.Y, (loadedTileX, loadedTileZ, success) =>
                {
                    // Mark tile as loaded when heightmap is ready
                    if (_activeTiles.TryGetValue(new Vector2Int(loadedTileX, loadedTileZ), out var tileEntity))
                    {
                        if (tileEntity.IsAlive() && tileEntity.Has<TerrainTileComponent>())
                        {
                            ref var tileComp = ref tileEntity.Get<TerrainTileComponent>();
                            tileComp.IsLoaded = success;
                        }
                    }
                });
            }
        }

        // Update distance from camera for all active tiles
        var query = new QueryDescription()
            .WithAll<TerrainTileComponent>();

        _world.Query(in query, (Entity entity, ref TerrainTileComponent tile) =>
        {
            // Calculate tile center position
            float tileCenterX = (tile.TileCoord.X + 0.5f) * VeldridTerrainManager.TileDimension;
            float tileCenterZ = (tile.TileCoord.Y + 0.5f) * VeldridTerrainManager.TileDimension;
            Vector3 tileCenter = new Vector3(tileCenterX, cameraPosition.Y, tileCenterZ);

            // Update distance (horizontal distance for LOD purposes)
            Vector2 cameraPosXZ = new Vector2(cameraPosition.X, cameraPosition.Z);
            Vector2 tileCenterXZ = new Vector2(tileCenterX, tileCenterZ);
            tile.DistanceFromCamera = Vector2.Distance(cameraPosXZ, tileCenterXZ);
        });
    }

    /// <summary>
    /// Get count of currently active tiles
    /// </summary>
    public int GetActiveTileCount() => _activeTiles.Count;

    /// <summary>
    /// Get count of loaded tiles (heightmap ready)
    /// </summary>
    public int GetLoadedTileCount()
    {
        int count = 0;
        var query = new QueryDescription()
            .WithAll<TerrainTileComponent>();

        _world.Query(in query, (ref TerrainTileComponent tile) =>
        {
            if (tile.IsLoaded)
                count++;
        });

        return count;
    }

    /// <summary>
    /// Process pending heightmap loads from VeldridTerrainManager and update tile components
    /// Must be called from main/render thread
    /// </summary>
    public void ProcessPendingLoads()
    {
        // Limit tile processing per frame to prevent freezing (GPU texture creation is expensive)
        const int maxTilesPerFrame = 10;
        int tilesProcessed = 0;

        // Process any pending tile loads from the terrain manager
        while (tilesProcessed < maxTilesPerFrame)
        {
            // Check limit BEFORE dequeuing to avoid losing tiles
            var pending = _terrainManager.DequeuePendingLoad();
            if (pending == null)
                break;

            // Convert from Rendering.Vector2Int to Editor.Vector2Int
            var tileCoord = new Vector2Int(pending.Coordinate.X, pending.Coordinate.Y);

            // Find the entity for this tile
            if (_activeTiles.TryGetValue(tileCoord, out var entity))
            {
                if (entity.IsAlive() && entity.Has<TerrainTileComponent>())
                {
                    ref var tileComp = ref entity.Get<TerrainTileComponent>();

                    // Create GPU texture from pre-converted float heightmap data (R32_Float format)
                    // HeightmapData was already converted on background thread
                    var heightTexture = VeldridTexture.CreateR32FromFloats(
                        _device,
                        pending.Width,
                        pending.Height,
                        pending.HeightmapData);

                    // Store both GPU texture and CPU-side data in component
                    tileComp.HeightTexture = heightTexture.Texture;
                    tileComp.HeightTextureView = heightTexture.TextureView;
                    tileComp.HeightmapData = pending.HeightmapData;
                    tileComp.HeightmapWidth = pending.Width;
                    tileComp.HeightmapHeight = pending.Height;
                    tileComp.HasGpuTexture = true;
                    // Note: HeightTextureResourceSet will be created by TerrainRenderingSystem on first render
                    tileComp.IsLoaded = true;

                    _logger.Information("Tile ({TileX}, {TileY}) heightmap loaded (R32_Float)", tileCoord.X, tileCoord.Y);
                }
            }

            tilesProcessed++;
        }

        // Log if we hit the limit (means more tiles queued)
        if (tilesProcessed >= maxTilesPerFrame)
        {
            _logger.Information("Processed {Count} tiles this frame (limit reached, more may be queued)", tilesProcessed);
        }
    }

    /// <summary>
    /// Get terrain height at world position by sampling heightmap (or base height if not loaded)
    /// </summary>
    public float GetTerrainHeightAt(Vector3 worldPos)
    {
        // Calculate which tile this position is in
        int tileX = (int)Math.Floor(worldPos.X / VeldridTerrainManager.TileDimension);
        int tileZ = (int)Math.Floor(-worldPos.Z / VeldridTerrainManager.TileDimension);  // Z is flipped
        Vector2Int tileCoord = new Vector2Int(tileX, tileZ);

        // Try to find the tile entity
        if (_activeTiles.TryGetValue(tileCoord, out var entity) && entity.IsAlive() && entity.Has<TerrainTileComponent>())
        {
            ref var tile = ref entity.Get<TerrainTileComponent>();

            // If tile has heightmap data, sample it
            if (tile.HeightmapData != null && tile.IsLoaded)
            {
                // Calculate position within tile (0 to TileDimension)
                float tileLocalX = worldPos.X - (tileX * VeldridTerrainManager.TileDimension);
                float tileLocalZ = -worldPos.Z - (tileZ * VeldridTerrainManager.TileDimension); // Z is flipped

                // Convert to heightmap texture coordinates (0 to width-1, 0 to height-1)
                float u = tileLocalX / VeldridTerrainManager.TileDimension; // 0 to 1
                float v = tileLocalZ / VeldridTerrainManager.TileDimension; // 0 to 1

                // Clamp to valid range
                u = Math.Clamp(u, 0.0f, 1.0f);
                v = Math.Clamp(v, 0.0f, 1.0f);

                // Sample heightmap (bilinear interpolation for smoothness)
                int width = tile.HeightmapWidth;
                int height = tile.HeightmapHeight;

                float x = u * (width - 1);
                float y = v * (height - 1);

                int x0 = (int)Math.Floor(x);
                int y0 = (int)Math.Floor(y);
                int x1 = Math.Min(x0 + 1, width - 1);
                int y1 = Math.Min(y0 + 1, height - 1);

                float fx = x - x0;
                float fy = y - y0;

                // Sample four corners
                float h00 = tile.HeightmapData[y0 * width + x0];
                float h10 = tile.HeightmapData[y0 * width + x1];
                float h01 = tile.HeightmapData[y1 * width + x0];
                float h11 = tile.HeightmapData[y1 * width + x1];

                // Bilinear interpolation
                float h0 = h00 * (1 - fx) + h10 * fx;
                float h1 = h01 * (1 - fx) + h11 * fx;
                float normalizedHeight = h0 * (1 - fy) + h1 * fy;

                // Convert normalized height to world height (same formula as shader)
                // height = (normalizedValue * 1000) + 500
                return (normalizedHeight * 1000.0f) + 500.0f;
            }
        }

        return 500.0f; // Base flat height if tile not loaded
    }

    public void Dispose()
    {
        // Cleanup active tiles
        foreach (var entity in _activeTiles.Values)
        {
            if (_world.IsAlive(entity) && entity.Has<TerrainTileComponent>())
            {
                ref var tile = ref entity.Get<TerrainTileComponent>();
                tile.HeightTextureResourceSet?.Dispose();
                tile.HeightTextureView?.Dispose();
                tile.HeightTexture?.Dispose();
                tile.HeightmapData = null; // Allow GC to collect
            }
        }
        _activeTiles.Clear();
    }
}
