using AlinasRailTools.Scripting;
using AlinasRailTools.Scripting.Turntables;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace AlinasRailTools.StrangeCustomsCompat.Extensions;

/// <summary>
/// StrangeCustoms JSON extension methods for TurntableInstanceBuilder
/// </summary>
public static class TurntableExtensions
{
  /// <summary>
  /// Apply StrangeCustoms JSON data to a TurntableInstanceBuilder
  /// </summary>
  public static TurntableInstanceBuilder ApplySCJSON(this TurntableInstanceBuilder builder, string id, JObject data)
  {
    // Prefab - use turntable template to create PrefabInstance
    var prefabTemplateName = "turntable"; // Default turntable template

    // Create PrefabInstance from template at 0,0,0 relative to turntable
    var prefabInstance = builder.Helper.Prefabs
      .Instance(id + ".prefab")
        .WithTemplate(prefabTemplateName)
        .WithParentObject(builder.Turntable.transform)
        .At(0, 0, 0)
        .WithRotation(Quaternion.identity);

    // Note: PrefabInstance is NOT activated yet - will be activated by Build() after nodes are attached
    builder.WithPrefabInstance(prefabInstance);

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

    // Radius
    var radius = SCCompatHelpers.ParseInt(data["Radius"]) ?? SCCompatHelpers.ParseInt(data["radius"]);
    if (radius.HasValue)
    {
      builder.WithRadius(radius.Value);
    }

    // Subdivisions
    var subdivisions = SCCompatHelpers.ParseInt(data["Subdivisions"]) ?? SCCompatHelpers.ParseInt(data["subdivisions"]);
    if (subdivisions.HasValue)
    {
      builder.WithSubdivisions(subdivisions.Value);
    }

    // Build the turntable (this will activate the PrefabInstance after nodes are attached)
    builder.Build();
    builder.SetActive(true);

    return builder;
  }
}
