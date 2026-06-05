using System.Linq;
using AlinasRailTools.Scripting;
using AlinasRailTools.Scripting.Turntables;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace AlinasRailTools.StrangeCustomsCompat.Extensions;

/// <summary>
/// StrangeCustoms JSON extension methods for RoundhouseInstanceBuilder
/// </summary>
public static class RoundhouseExtensions
{
  /// <summary>
  /// Apply StrangeCustoms JSON data to a RoundhouseInstanceBuilder
  /// </summary>
  public static RoundhouseInstanceBuilder ApplySCJSON(this RoundhouseInstanceBuilder builder, string id, JObject data)
  {
    // Position and Rotation
    var position = SCCompatHelpers.ParseVector3(data["Position"]);
    if (position == Vector3.zero && data["position"] != null)
    {
      position = SCCompatHelpers.ParseVector3(data["position"]);
    }
    if (position != Vector3.zero)
    {
      builder.At(position);
    }

    var rotation = SCCompatHelpers.ParseVector3(data["Rotation"]);
    if (rotation == Vector3.zero && data["rotation"] != null)
    {
      rotation = SCCompatHelpers.ParseVector3(data["rotation"]);
    }
    if (rotation != Vector3.zero)
    {
      builder.WithRotation(rotation);
    }

    // Subdivisions
    var subdivisions = SCCompatHelpers.ParseInt(data["Subdivisions"]) ?? SCCompatHelpers.ParseInt(data["subdivisions"]);
    if (subdivisions.HasValue)
    {
      builder.WithSubdivisions(subdivisions.Value);
    }

    // Stalls
    var stalls = SCCompatHelpers.ParseInt(data["RoundhouseStalls"])
                 ?? SCCompatHelpers.ParseInt(data["roundhouseStalls"])
                 ?? SCCompatHelpers.ParseInt(data["Stalls"])
                 ?? SCCompatHelpers.ParseInt(data["stalls"]);
    if (stalls.HasValue)
    {
      builder.WithStalls(stalls.Value);
    }

    // Track Length
    var trackLength = SCCompatHelpers.ParseInt(data["RoundhouseTrackLength"])
                      ?? SCCompatHelpers.ParseInt(data["roundhouseTrackLength"])
                      ?? SCCompatHelpers.ParseInt(data["TrackLength"])
                      ?? SCCompatHelpers.ParseInt(data["trackLength"]);
    if (trackLength.HasValue)
    {
      builder.WithTrackLength(trackLength.Value);
    }

    // Individual piece templates - parse vanilla:// URIs and get template GameObjects
    // Only override defaults if explicitly specified in JSON
    var stallPrefabName = ParsePrefabName(data["StallPrefab"]) ?? ParsePrefabName(data["stallPrefab"]);
    if (!string.IsNullOrEmpty(stallPrefabName))
    {
      var template = builder.Helper.Prefabs.GetTemplate(stallPrefabName).Template;
      builder.WithStallTemplate(template);
    }

    var sidePrefabName = ParsePrefabName(data["SidePrefab"]) ?? ParsePrefabName(data["sidePrefab"]);
    if (!string.IsNullOrEmpty(sidePrefabName))
    {
      var template = builder.Helper.Prefabs.GetTemplate(sidePrefabName).Template;
      builder.WithSideTemplate(template);
    }

    var betweenPrefabName = ParsePrefabName(data["BetweenPrefab"]) ?? ParsePrefabName(data["betweenPrefab"]);
    if (!string.IsNullOrEmpty(betweenPrefabName))
    {
      var template = builder.Helper.Prefabs.GetTemplate(betweenPrefabName).Template;
      builder.WithBetweenTemplate(template);
    }

    // Turntable reference - look up by identifier
    var turntableId = SCCompatHelpers.ParseString(data["Turntable"])
                      ?? SCCompatHelpers.ParseString(data["turntable"]);
    if (!string.IsNullOrEmpty(turntableId))
    {
      var turntable = TurntableBuilder.AllTurntables
        .Values
        .FirstOrDefault(t => t.identifier == turntableId);
      if (turntable != null)
      {
        builder.WithTurntable(turntable);
      }
    }

    // Build the roundhouse
    builder.Build();
    builder.SetActive(true);

    return builder;
  }

  /// <summary>
  /// Parse prefab name from JSON, stripping vanilla:// prefix if present
  /// </summary>
  private static string ParsePrefabName(JToken token)
  {
    var prefabName = SCCompatHelpers.ParseString(token);
    if (string.IsNullOrEmpty(prefabName))
    {
      return null;
    }

    // Strip vanilla:// prefix
    if (prefabName.StartsWith("vanilla://"))
    {
      prefabName = prefabName.Substring("vanilla://".Length);
    }

    return prefabName;
  }
}
