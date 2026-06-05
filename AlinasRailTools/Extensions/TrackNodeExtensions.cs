using AlinasRailTools.Shared.Definitions;
using Track;

namespace AlinasRailTools.Extensions;

/// <summary>
/// Serialization extensions for TrackNode
/// </summary>
public static class TrackNodeExtensions
{
    /// <summary>
    /// Serialize a TrackNode to its serialized representation
    /// </summary>
    public static SerializedTrackNode Serialize(this TrackNode node)
    {
        return new SerializedTrackNode
        {
            Position = node.transform.localPosition.ToSerialized(),
            Rotation = node.transform.localEulerAngles.ToSerialized(),
            FlipSwitchStand = node.flipSwitchStand
        };
    }

    /// <summary>
    /// Apply serialized data to a TrackNode
    /// </summary>
    public static void ApplyFrom(this TrackNode node, SerializedTrackNode data)
    {
        node.transform.localPosition = data.Position.ToUnity();
        node.transform.localEulerAngles = data.Rotation.ToUnity();
        node.flipSwitchStand = data.FlipSwitchStand;
    }
}
