using AlinasRailTools.Scripting.Graph;
using Newtonsoft.Json.Linq;
using Track;

namespace AlinasRailTools.StrangeCustomsCompat.Extensions;

/// <summary>
/// StrangeCustoms JSON extension methods for TrackSegmentBuilder
/// </summary>
public static class TrackSegmentExtensions
{
    /// <summary>
    /// Apply StrangeCustoms JSON data to a TrackSegmentBuilder
    /// </summary>
    public static TrackSegmentBuilder ApplySCJSON(this TrackSegmentBuilder builder, string id, JObject data)
    {
        // Style
        var style = SCCompatHelpers.ParseString(data["style"]);
        if (!string.IsNullOrEmpty(style))
        {
            if (System.Enum.TryParse<TrackSegment.Style>(style, out var styleEnum))
            {
                builder.WithStyle(styleEnum);
            }
        }

        // Priority
        var priority = SCCompatHelpers.ParseInt(data["priority"]);
        if (priority.HasValue)
        {
            builder.WithPriority(priority.Value);
        }

        // GroupId
        var groupId = SCCompatHelpers.ParseString(data["groupId"]);
        if (!string.IsNullOrEmpty(groupId))
        {
            builder.WithGroupId(groupId);
        }

        builder.SetActive(true);
        return builder;
    }
}
