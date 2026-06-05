using System.Linq;
using AlinasRailTools.Scripting;
using Model.Ops;
using Newtonsoft.Json.Linq;
using RollingStock.Controls;
using Track;
using UnityEngine;

namespace AlinasRailTools.StrangeCustomsCompat.Extensions;

/// <summary>
/// StrangeCustoms JSON extension methods for LoaderInstanceBuilder
/// </summary>
public static class LoaderExtensions
{
  /// <summary>
  /// Apply StrangeCustoms JSON data to a LoaderInstanceBuilder
  /// </summary>
  public static LoaderInstanceBuilder ApplySCJSON(this LoaderInstanceBuilder builder, string id, JObject data)
  {
    // Prefab - strip "vanilla://" prefix if present and create PrefabInstance
    var prefabTemplateName = SCCompatHelpers.ParseString(data["prefab"]);
    if (!string.IsNullOrEmpty(prefabTemplateName))
    {
      // Strip vanilla:// prefix
      if (prefabTemplateName.StartsWith("vanilla://"))
      {
        prefabTemplateName = prefabTemplateName.Substring("vanilla://".Length);
      }

      // Create PrefabInstance from template at 0,0,0 relative to loader
      var prefabInstance = builder.Helper.Prefabs
        .Instance(id + ".prefab")
          .WithTemplate(prefabTemplateName)
          .WithParentObject(builder.Loader.transform)
          .At(0, 0, 0)
          .WithRotation(Quaternion.identity);

      var instance = prefabInstance.Instance;

      // Disable track marker if present
      var trackMarker = instance.GetComponent<TrackMarker>();
      if (trackMarker != null) trackMarker.enabled = false;

      // Set global object ID
      var gkvo = instance.GetComponent<GlobalKeyValueObject>();
      if (gkvo != null)
      {
        gkvo.globalObjectId = id + ".loader";
      }

      // Enable all renderers
      foreach (var r in instance.GetComponentsInChildren<Renderer>())
      {
        r.enabled = true;
      }

      builder.WithPrefabInstance(prefabInstance);
    }

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

    // Industry (optional) - look up by ID
    var industryId = SCCompatHelpers.ParseString(data["Industry"])
                     ?? SCCompatHelpers.ParseString(data["industry"]);
    if (!string.IsNullOrEmpty(industryId))
    {
      var industry = UnityEngine.Object.FindObjectsByType<Industry>(FindObjectsSortMode.None)
        .FirstOrDefault(i => i.identifier == industryId);
      if (industry != null)
      {
        builder.WithIndustry(industry);
      }
    }

    builder.SetActive(true);
    return builder;
  }
}
