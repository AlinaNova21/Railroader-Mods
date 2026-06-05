using AlinasRailTools.Scripting.Ops;
using Model.Definition.Data;
using Newtonsoft.Json.Linq;

namespace AlinasRailTools.StrangeCustomsCompat.Extensions;

/// <summary>
/// StrangeCustoms JSON extension methods for LoadBuilder
/// </summary>
public static class LoadExtensions
{
    /// <summary>
    /// Apply StrangeCustoms JSON data to a LoadBuilder
    /// </summary>
    public static LoadBuilder ApplySCJSON(this LoadBuilder builder, string id, JObject data)
    {
        // Description
        var description = SCCompatHelpers.ParseString(data["description"]);
        if (!string.IsNullOrEmpty(description))
        {
            builder.WithDescription(description);
        }

        // Units
        var units = SCCompatHelpers.ParseString(data["units"]);
        if (!string.IsNullOrEmpty(units))
        {
            if (System.Enum.TryParse<LoadUnits>(units, out var unitsEnum))
            {
                builder.WithUnits(unitsEnum);
            }
        }

        // Density
        var density = SCCompatHelpers.ParseFloat(data["density"]);
        if (density.HasValue)
        {
            builder.WithDensity(density.Value);
        }

        builder.SetActive(true);
        return builder;
    }
}
