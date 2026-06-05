using AlinasRailTools.Editor.Rendering;

namespace AlinasRailTools.Editor.ECS.Components;

/// <summary>
/// Component that combines mesh reference and material for rendering
/// This replaces the separate MeshComponent + MaterialComponent pattern
/// </summary>
public struct MeshRendererComponent
{
    /// <summary>
    /// Unique ID of the mesh to render (looked up in MeshManager)
    /// </summary>
    public Guid MeshId;

    /// <summary>
    /// Material defining shaders and rendering properties
    /// </summary>
    public Material Material;

    /// <summary>
    /// Whether this renderer is enabled and should be rendered
    /// LOD system can toggle this based on distance
    /// </summary>
    public bool Enabled;

    /// <summary>
    /// LOD level this renderer belongs to (0 = highest quality, higher = lower quality)
    /// Used by LOD system to determine which renderers to enable/disable
    /// </summary>
    public int LODLevel;

    /// <summary>
    /// Whether this entity casts shadows (future feature)
    /// </summary>
    public bool CastsShadows;

    /// <summary>
    /// Whether this entity receives shadows (future feature)
    /// </summary>
    public bool ReceivesShadows;

    public MeshRendererComponent(Guid meshId, Material material, int lodLevel = 0)
    {
        MeshId = meshId;
        Material = material;
        Enabled = true;
        LODLevel = lodLevel;
        CastsShadows = true;
        ReceivesShadows = true;
    }

    public MeshRendererComponent(Guid meshId, Material material, int lodLevel, bool castsShadows, bool receivesShadows)
    {
        MeshId = meshId;
        Material = material;
        Enabled = true;
        LODLevel = lodLevel;
        CastsShadows = castsShadows;
        ReceivesShadows = receivesShadows;
    }
}
