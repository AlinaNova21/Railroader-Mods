using AlinasRailTools.Scripting.Ops;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace AlinasRailTools.StrangeCustomsCompat.Extensions;

/// <summary>
/// StrangeCustoms JSON extension methods for SceneryAssetInstanceBuilder
/// </summary>
public static class SceneryExtensions
{
    /// <summary>
    /// Apply StrangeCustoms JSON data to a SceneryAssetInstanceBuilder
    /// </summary>
    public static SceneryAssetInstanceBuilder ApplySCJSON(this SceneryAssetInstanceBuilder builder, string id, JObject data)
    {
        // ModelIdentifier
        var modelIdentifier = SCCompatHelpers.ParseString(data["modelIdentifier"]);
        if (!string.IsNullOrEmpty(modelIdentifier))
        {
            builder.ModelIdentifier(modelIdentifier);
        }

        // Position
        var position = SCCompatHelpers.ParseVector3(data["position"]);
        if (position != Vector3.zero)
        {
            builder.Position(position);
        }

        // Rotation (optional)
        if (data.ContainsKey("rotation"))
        {
            var rotation = SCCompatHelpers.ParseVector3(data["rotation"]);
            builder.Rotation(rotation);
        }

        // Scale
        var scale = SCCompatHelpers.ParseVector3(data["scale"], Vector3.one);
        if (scale != Vector3.one)
        {
            builder.Scale(scale);
        }

        builder.SetActive(true);
        return builder;
    }
}
