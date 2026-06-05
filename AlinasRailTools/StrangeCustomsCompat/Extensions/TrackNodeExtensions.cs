using AlinasRailTools.Scripting.Graph;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace AlinasRailTools.StrangeCustomsCompat.Extensions;

/// <summary>
/// StrangeCustoms JSON extension methods for TrackNodeBuilder
/// </summary>
public static class TrackNodeExtensions
{
    /// <summary>
    /// Apply StrangeCustoms JSON data to a TrackNodeBuilder
    /// </summary>
    public static TrackNodeBuilder ApplySCJSON(this TrackNodeBuilder builder, string id, JObject data)
    {
        // Position
        var position = SCCompatHelpers.ParseVector3(data["position"]);
        if (position != Vector3.zero)
        {
            builder.At(position);
        }

        // Rotation
        var rotation = SCCompatHelpers.ParseVector3(data["rotation"]);
        if (rotation != Vector3.zero)
        {
            builder.WithRotation(rotation);
        }

        // FlipSwitchStand
        var flipSwitchStand = SCCompatHelpers.ParseBool(data["flipSwitchStand"]);
        if (flipSwitchStand.HasValue)
        {
            builder.WithFlipSwitchStand(flipSwitchStand.Value);
        }

        builder.SetActive(true);
        return builder;
    }
}
