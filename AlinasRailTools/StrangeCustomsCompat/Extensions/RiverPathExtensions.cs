using AlinasRailTools.Scripting.Rivers;
using Helpers;
using Map.Runtime.MaskComponents;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace AlinasRailTools.StrangeCustomsCompat.Extensions;

/// <summary>
/// StrangeCustoms JSON extension methods for RiverPathBuilder
/// </summary>
public static class RiverPathExtensions
{
    /// <summary>
    /// Apply StrangeCustoms JSON data to a RiverPathBuilder
    /// </summary>
    public static RiverPathBuilder ApplySCJSON(this RiverPathBuilder builder, string name, JObject data)
    {
        Debug.Log($"SCCompat: Applying RiverPath '{name}'");

        // Profile
        var profileName = SCCompatHelpers.ParseString(data["profile"]);
        if (!string.IsNullOrEmpty(profileName))
        {
            builder.WithProfile(profileName);
        }

        // Style
        var styleStr = SCCompatHelpers.ParseString(data["style"]);
        if (!string.IsNullOrEmpty(styleStr))
        {
            if (System.Enum.TryParse<RiverPath.RiverPathStyle>(styleStr, out var style))
            {
                builder.WithStyle(style);
            }
        }

        // YOffset (optional)
        var yOffset = SCCompatHelpers.ParseFloat(data["yOffset"]);
        if (yOffset.HasValue)
        {
            builder.WithYOffset(yOffset.Value);
        }

        // Points
        var pointsArray = data["points"]?.ToObject<JArray>();
        if (pointsArray != null && pointsArray.Count > 0)
        {
            var points = new System.Collections.Generic.List<RiverPath.Point>();

            foreach (var pointToken in pointsArray)
            {
                var pointData = pointToken.ToObject<JObject>();
                if (pointData != null)
                {
                    var position = SCCompatHelpers.ParseVector3(pointData["position"]);
                    var rotation = SCCompatHelpers.ParseVector3(pointData["rotation"]);
                    var width = SCCompatHelpers.ParseFloat(pointData["width"]) ?? 1f;

                    points.Add(new RiverPath.Point
                    {
                        position = position,
                        eulerAngles = rotation,
                        width = width
                    });
                }
            }

            builder.SetPoints(points.ToArray());
        }

        builder.SetActive(true);
        return builder;
    }
}
