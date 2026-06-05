using AlinasRailTools.Shared.Definitions;
using Track;

namespace AlinasRailTools.Extensions;

/// <summary>
/// Serialization extensions for TrackSpan
/// </summary>
public static class TrackSpanExtensions
{
    /// <summary>
    /// Serialize a TrackSpan to its serialized representation
    /// </summary>
    public static SerializedTrackSpan Serialize(this TrackSpan span)
    {
        return new SerializedTrackSpan
        {
            Upper = SerializeLocation(span.upper),
            Lower = SerializeLocation(span.lower)
        };
    }

    /// <summary>
    /// Serialize a Track.Location to TrackSpanPart
    /// </summary>
    private static TrackSpanPart SerializeLocation(object location)
    {
        if (location == null) return null;

        // Use dynamic to access properties since Location type might not be accessible
        dynamic loc = location;

        return new TrackSpanPart
        {
            SegmentId = loc.segment?.id ?? "",
            Distance = loc.distance,
            End = loc.end == 0 ? TrackSpanPartEnd.Start : TrackSpanPartEnd.End
        };
    }

    /// <summary>
    /// Apply serialized data to a TrackSpan
    /// Note: Requires segments to already exist in the scene
    /// </summary>
    public static void ApplyFrom(this TrackSpan span, SerializedTrackSpan data, System.Func<string, TrackSegment> getSegment)
    {
        span.upper = (Location?)DeserializeLocation(data.Upper, getSegment);
        span.lower = (Location?)DeserializeLocation(data.Lower, getSegment);
    }

    /// <summary>
    /// Deserialize a TrackSpanPart to Track.Location
    /// </summary>
    private static object DeserializeLocation(TrackSpanPart part, System.Func<string, TrackSegment> getSegment)
    {
        if (part == null) return null;

        var segment = getSegment(part.SegmentId);
        var end = part.End == TrackSpanPartEnd.End ? TrackSegment.End.B : TrackSegment.End.A;

        return new Location(segment, part.Distance, end);
    }
}
