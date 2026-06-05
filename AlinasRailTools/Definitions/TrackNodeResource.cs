using System.Numerics;
using AlinasRailTools.Core.Attributes;

namespace AlinasRailTools.Definitions;

/// <summary>
/// Represents a track node (junction, endpoint, or switch) in the world.
/// Track nodes are foundation-level resources with no dependencies.
/// </summary>
[ResourceType("trackNodes")]
public class TrackNodeResource : BaseResource
{
    /// <summary>
    /// 3D position of the track node in world coordinates
    /// </summary>
    public Vector3 Position { get; set; } = Vector3.Zero;

    /// <summary>
    /// 3D rotation of the track node in degrees
    /// </summary>
    public Vector3 Rotation { get; set; } = Vector3.Zero;

    /// <summary>
    /// Whether to flip the switch stand orientation (for switches)
    /// </summary>
    public bool FlipSwitchStand { get; set; } = false;
}