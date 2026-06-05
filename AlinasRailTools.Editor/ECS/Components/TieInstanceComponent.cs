using System.Numerics;

namespace AlinasRailTools.Editor.ECS.Components;

/// <summary>
/// Component that stores instance data for railroad ties
/// Each track segment can have multiple ties
/// </summary>
public struct TieInstanceComponent
{
    /// <summary>
    /// Array of tie transforms (4x4 matrices in world space)
    /// </summary>
    public Matrix4x4[] InstanceTransforms;

    public TieInstanceComponent(Matrix4x4[] instanceTransforms)
    {
        InstanceTransforms = instanceTransforms;
    }
}
