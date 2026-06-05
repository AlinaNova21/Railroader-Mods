using Veldrid;

namespace AlinasRailTools.Editor.ECS.Components;

/// <summary>
/// Component representing a terrain tile with heightmap data
/// Transform is calculated automatically from tile position
/// </summary>
public struct TerrainTileComponent
{
    /// <summary>
    /// Tile coordinate (X, Y where Y is the Z coordinate in world space)
    /// </summary>
    public Vector2Int TileCoord;

    /// <summary>
    /// Whether the tile heightmap data is loaded (CPU-side)
    /// </summary>
    public bool IsLoaded;

    /// <summary>
    /// Whether the GPU texture has been created for this tile
    /// True means HeightTexture/HeightTextureView are valid
    /// </summary>
    public bool HasGpuTexture;

    /// <summary>
    /// Distance from camera (for culling/LOD)
    /// </summary>
    public float DistanceFromCamera;

    /// <summary>
    /// GPU texture containing heightmap data (R16 format, 513x513)
    /// Null until loaded
    /// </summary>
    public Texture? HeightTexture;

    /// <summary>
    /// GPU texture view for sampling in shaders
    /// </summary>
    public TextureView? HeightTextureView;

    /// <summary>
    /// Cached resource set for texture binding (set 1)
    /// Created once per tile to avoid recreating every frame
    /// </summary>
    public ResourceSet? HeightTextureResourceSet;

    /// <summary>
    /// CPU-side copy of heightmap data (normalized 0-1 values)
    /// Used for terrain height queries by camera system
    /// </summary>
    public float[]? HeightmapData;

    /// <summary>
    /// Width of the heightmap (typically 513)
    /// </summary>
    public int HeightmapWidth;

    /// <summary>
    /// Height of the heightmap (typically 513)
    /// </summary>
    public int HeightmapHeight;

    public TerrainTileComponent(int tileX, int tileZ)
    {
        TileCoord = new Vector2Int(tileX, tileZ);
        IsLoaded = false;
        HasGpuTexture = false;
        DistanceFromCamera = float.MaxValue;
        HeightTexture = null;
        HeightTextureView = null;
        HeightTextureResourceSet = null;
        HeightmapData = null;
        HeightmapWidth = 0;
        HeightmapHeight = 0;
    }
}
