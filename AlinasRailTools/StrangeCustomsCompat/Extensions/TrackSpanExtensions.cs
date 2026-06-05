using AlinasRailTools.Scripting.Graph;
using Newtonsoft.Json.Linq;
using Track;

namespace AlinasRailTools.StrangeCustomsCompat.Extensions;

/// <summary>
/// StrangeCustoms JSON extension methods for TrackSpanBuilder
/// </summary>
public static class TrackSpanExtensions
{
    /// <summary>
    /// Apply StrangeCustoms JSON data to a TrackSpanBuilder
    /// </summary>
    public static TrackSpanBuilder ApplySCJSON(this TrackSpanBuilder builder, string id, JObject data)
    {
        // Upper location
        var upperData = data["upper"]?.ToObject<JObject>();
        if (upperData != null)
        {
            var segmentId = SCCompatHelpers.ParseString(upperData["segmentId"]);
            var distance = SCCompatHelpers.ParseFloat(upperData["distance"]) ?? 0f;
            var endStr = SCCompatHelpers.ParseString(upperData["end"]);

            if (!string.IsNullOrEmpty(segmentId))
            {
                var segmentBuilder = builder.Helper.Graph.GetTrackSegment(segmentId);
                var end = endStr == "End" ? TrackSegment.End.B : TrackSegment.End.A;
                var location = segmentBuilder.LocationFromDistance(distance, end);
                builder.WithUpper(location);
            }
        }

        // Lower location
        var lowerData = data["lower"]?.ToObject<JObject>();
        if (lowerData != null)
        {
            var segmentId = SCCompatHelpers.ParseString(lowerData["segmentId"]);
            var distance = SCCompatHelpers.ParseFloat(lowerData["distance"]) ?? 0f;
            var endStr = SCCompatHelpers.ParseString(lowerData["end"]);

            if (!string.IsNullOrEmpty(segmentId))
            {
                var segmentBuilder = builder.Helper.Graph.GetTrackSegment(segmentId);
                var end = endStr == "End" ? TrackSegment.End.B : TrackSegment.End.A;
                var location = segmentBuilder.LocationFromDistance(distance, end);
                builder.WithLower(location);
            }
        }

        builder.SetActive(true);
        return builder;
    }
}
