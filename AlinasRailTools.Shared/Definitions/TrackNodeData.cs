using System.Numerics;

namespace AlinasRailTools.Shared.Definitions;

/// <summary>
/// Database-backed track node data
/// </summary>
public class TrackNodeData
{
    public string Id { get; set; } = string.Empty;
    public Vector3 Position { get; set; } = Vector3.Zero;
    public Vector3 Rotation { get; set; } = Vector3.Zero;  // Euler angles in degrees
    public string Type { get; set; }
}
