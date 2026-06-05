using Newtonsoft.Json.Linq;
using UnityEngine;

namespace AlinasRailTools.StrangeCustomsCompat;

/// <summary>
/// Shared utilities for StrangeCustoms JSON parsing
/// </summary>
public static class SCCompatHelpers
{
    /// <summary>
    /// Parse a Vector3 from a JToken, supporting both object format {x,y,z} and null
    /// </summary>
    public static Vector3 ParseVector3(JToken vectorData, Vector3 defaultValue = default)
    {
        if (vectorData == null) return defaultValue;

        var x = vectorData["x"]?.ToObject<float>() ?? defaultValue.x;
        var y = vectorData["y"]?.ToObject<float>() ?? defaultValue.y;
        var z = vectorData["z"]?.ToObject<float>() ?? defaultValue.z;
        return new Vector3(x, y, z);
    }

    /// <summary>
    /// Parse a Color from a JToken, supporting both array [r,g,b,a] and object {r,g,b,a} formats
    /// </summary>
    public static Color? ParseColor(JToken colorData)
    {
        if (colorData == null) return null;

        if (colorData.Type == JTokenType.Array)
        {
            var colorArray = colorData.ToObject<float[]>();
            if (colorArray == null || colorArray.Length < 3) return null;

            var r = colorArray[0];
            var g = colorArray[1];
            var b = colorArray[2];
            var a = colorArray.Length > 3 ? colorArray[3] : 1f;
            return new Color(r, g, b, a);
        }
        else if (colorData.Type == JTokenType.Object)
        {
            var r = colorData["r"]?.ToObject<float>() ?? 1f;
            var g = colorData["g"]?.ToObject<float>() ?? 1f;
            var b = colorData["b"]?.ToObject<float>() ?? 1f;
            var a = colorData["a"]?.ToObject<float>() ?? 1f;
            return new Color(r, g, b, a);
        }

        return null;
    }

    /// <summary>
    /// Parse a nullable boolean from a JToken
    /// </summary>
    public static bool? ParseBool(JToken boolData)
    {
        return boolData?.ToObject<bool>();
    }

    /// <summary>
    /// Parse a nullable int from a JToken
    /// </summary>
    public static int? ParseInt(JToken intData)
    {
        return intData?.ToObject<int>();
    }

    /// <summary>
    /// Parse a nullable float from a JToken
    /// </summary>
    public static float? ParseFloat(JToken floatData)
    {
        return floatData?.ToObject<float>();
    }

    /// <summary>
    /// Parse a string from a JToken
    /// </summary>
    public static string ParseString(JToken stringData)
    {
        return stringData?.ToString();
    }

    /// <summary>
    /// Strip the file() wrapper from a string if present
    /// Example: "file(game-graph.json)" -> "game-graph.json"
    /// </summary>
    public static string StripFileWrapper(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var trimmed = value.Trim();

        // Check if it starts with "file(" and ends with ")"
        if (trimmed.StartsWith("file(") && trimmed.EndsWith(")"))
        {
            // Extract the content between file( and )
            return trimmed.Substring(5, trimmed.Length - 6);
        }

        return value;
    }
}
