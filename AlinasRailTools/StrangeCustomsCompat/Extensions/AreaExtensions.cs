using AlinasRailTools.Scripting.Ops;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace AlinasRailTools.StrangeCustomsCompat.Extensions;

/// <summary>
/// StrangeCustoms JSON extension methods for AreaBuilder
/// </summary>
public static class AreaExtensions
{
    /// <summary>
    /// Apply StrangeCustoms JSON data to an AreaBuilder
    /// </summary>
    public static AreaBuilder ApplySCJSON(this AreaBuilder builder, string id, JObject data)
    {
        // Position
        var position = SCCompatHelpers.ParseVector3(data["position"]);
        if (position != Vector3.zero)
        {
            builder.At(position);
        }

        // Radius
        var radius = SCCompatHelpers.ParseFloat(data["radius"]);
        if (radius.HasValue)
        {
            builder.WithRadius(radius.Value);
        }

        // Name
        var name = SCCompatHelpers.ParseString(data["name"]);
        if (!string.IsNullOrEmpty(name))
        {
            builder.WithName(name);
        }

        // TagColor
        var tagColor = SCCompatHelpers.ParseColor(data["tagColor"]);
        if (tagColor.HasValue)
        {
            builder.WithTagColor(tagColor.Value);
        }

        builder.SetActive(true);

        // Apply Industries
        var industries = data["industries"]?.ToObject<JObject>();
        if (industries != null)
        {
            UnityEngine.Debug.Log($"SCCompat: Processing {industries.Count} Industries in Area '{id}'");

            foreach (var industry in industries)
            {
                var industryId = industry.Key;
                var industryData = industry.Value?.ToObject<JObject>();
                if (industryData != null)
                {
                    builder.Industry(industryId).ApplySCJSON(industryId, industryData);
                }
                else
                {
                    UnityEngine.Debug.Log($"SCCompat: Removing Industry '{industryId}' from Area '{id}'");
                    builder.RemoveIndustry(industryId);
                }
            }
        }

        return builder;
    }
}
