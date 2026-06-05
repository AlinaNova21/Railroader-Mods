using AlinasRailTools.Shared.Definitions;
using Track;

namespace AlinasRailTools.Extensions;

/// <summary>
/// Serialization extensions for TrackSegment
/// </summary>
public static class TrackSegmentExtensions
{
    /// <summary>
    /// Serialize a TrackSegment to its serialized representation
    /// </summary>
    public static SerializedTrackSegment Serialize(this TrackSegment segment)
    {
        return new SerializedTrackSegment
        {
            StartId = segment.a.id,
            EndId = segment.b.id,
            Style = (TrackSegmentStyle)segment.style,
            GroupId = segment.groupId,
            Priority = segment.priority
        };
    }

    /// <summary>
    /// Apply serialized data to a TrackSegment
    /// Note: Requires nodes to already exist in the scene
    /// </summary>
    public static void ApplyFrom(this TrackSegment segment, SerializedTrackSegment data, System.Func<string, TrackNode> getNode)
    {
        segment.a = getNode(data.StartId);
        segment.b = getNode(data.EndId);
        segment.style = (TrackSegment.Style)data.Style;
        segment.groupId = data.GroupId ?? "";
        segment.priority = data.Priority;
    }
}
