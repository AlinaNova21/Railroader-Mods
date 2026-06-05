using System.Numerics;
using AlinasRailTools.Editor.Rendering;

namespace AlinasRailTools.Editor.ECS.Components;

/// <summary>
/// Component for rendering multiple instances of the same mesh with individual transforms
/// Used for efficient batch rendering of repeated geometry (e.g., railroad ties)
/// The InstancedRenderSystem will automatically batch these instances
/// </summary>
public struct MeshRendererListComponent
{
    /// <summary>
    /// Mesh to render for all instances
    /// </summary>
    public Guid MeshId;

    /// <summary>
    /// Material to use for rendering
    /// Must have SupportsInstancing = true
    /// </summary>
    public Material Material;

    /// <summary>
    /// Individual transforms for each instance (in local space relative to entity's Transform)
    /// The InstancedRenderSystem will combine these with the parent entity's Transform
    /// </summary>
    public Matrix4x4[] InstanceTransforms;

    /// <summary>
    /// Whether this renderer is enabled
    /// </summary>
    public bool Enabled;

    /// <summary>
    /// LOD level for this renderer (0 = highest quality)
    /// </summary>
    public int LODLevel;

    public MeshRendererListComponent(Guid meshId, Material material, Matrix4x4[] instanceTransforms, int lodLevel = 0)
    {
        MeshId = meshId;
        Material = material;
        InstanceTransforms = instanceTransforms;
        Enabled = true;
        LODLevel = lodLevel;
    }
}
