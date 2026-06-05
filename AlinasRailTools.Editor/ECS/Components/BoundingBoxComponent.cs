using AlinasRailTools.Editor.Rendering;

namespace AlinasRailTools.Editor.ECS.Components;

/// <summary>
/// Component that stores bounding box for frustum culling
/// </summary>
public struct BoundingBoxComponent
{
    public BoundingBox Bounds;

    public BoundingBoxComponent(BoundingBox bounds)
    {
        Bounds = bounds;
    }
}
