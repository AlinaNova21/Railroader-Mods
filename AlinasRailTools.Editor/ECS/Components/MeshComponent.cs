using AlinasRailTools.Editor.Rendering;

namespace AlinasRailTools.Editor.ECS.Components;

/// <summary>
/// Component that references a VeldridMesh for rendering.
/// Multiple entities can share the same mesh instance.
/// </summary>
public struct MeshComponent
{
    /// <summary>
    /// Reference to the VeldridMesh (vertex and index buffers).
    /// This is a shared resource - multiple entities can reference the same mesh.
    /// </summary>
    public VeldridMesh Mesh;

    public MeshComponent(VeldridMesh mesh)
    {
        Mesh = mesh;
    }
}
