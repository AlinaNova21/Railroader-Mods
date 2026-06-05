using System.Numerics;

namespace AlinasRailTools.Editor.Track;

/// <summary>
/// Runtime representation of a track node with 3D position and rotation
/// </summary>
public class TrackNodeData
{
    /// <summary>
    /// Unique identifier for this node
    /// </summary>
    public string Id { get; set; } = "";

    /// <summary>
    /// 3D position in world space
    /// </summary>
    public Vector3 Position { get; set; }

    /// <summary>
    /// Rotation as Euler angles (degrees)
    /// </summary>
    public Vector3 Rotation { get; set; }

    /// <summary>
    /// Whether the switch stand is flipped
    /// </summary>
    public bool FlipSwitchStand { get; set; }

    /// <summary>
    /// Segments connected to this node (start or end)
    /// Populated by TrackGraph after loading
    /// </summary>
    public List<string> ConnectedSegmentIds { get; set; } = new();

    /// <summary>
    /// Get 2D position (XZ plane) for spatial indexing
    /// </summary>
    public Vector2 PositionXZ => new Vector2(Position.X, Position.Z);
}
