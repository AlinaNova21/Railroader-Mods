using AlinasRailTools.Editor.Rendering;

namespace AlinasRailTools.Editor.ECS.Components;

/// <summary>
/// Component for track segment LOD (Level of Detail)
/// Stores both detailed and simple line meshes for distance-based rendering
/// </summary>
public struct TrackLODComponent
{
    /// <summary>
    /// Simple line mesh for distant view (single silver line)
    /// </summary>
    public VeldridMesh? SimpleMesh;

    /// <summary>
    /// Distance at which to start fading out detailed mesh (in meters)
    /// </summary>
    public const float FadeStartDistance = 500.0f;

    /// <summary>
    /// Distance at which detailed mesh is fully faded out (in meters)
    /// </summary>
    public const float FadeEndDistance = 550.0f;

    public TrackLODComponent(VeldridMesh? simpleMesh)
    {
        SimpleMesh = simpleMesh;
    }
}
