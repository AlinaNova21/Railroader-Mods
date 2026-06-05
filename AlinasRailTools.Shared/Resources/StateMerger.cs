using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace AlinasRailTools.Shared.Resources;

/// <summary>
/// Merges state files by overlaying new states onto base states
/// </summary>
public class StateMerger
{
  /// <summary>
  /// Overlay newState onto baseState, returning merged result.
  /// - Resources in newState are added or updated
  /// - null values in newState = deletion
  /// - Resources only in baseState are kept
  /// - Nested objects are deep merged
  /// </summary>
  public StateFile Overlay(StateFile baseState, StateFile newState)
  {
    var result = baseState.Clone();

    foreach (var typeEntry in newState.Resources)
    {
      var typeName = typeEntry.Key;
      var newResources = typeEntry.Value;

      if (!result.Resources.ContainsKey(typeName))
      {
        result.Resources[typeName] = new Dictionary<string, JObject>();
      }

      foreach (var resourceEntry in newResources)
      {
        var id = resourceEntry.Key;
        var newData = resourceEntry.Value;

        if (newData == null)
        {
          // Explicit deletion
          result.Resources[typeName].Remove(id);
        }
        else if (result.Resources[typeName].TryGetValue(id, out var existingData))
        {
          // Merge existing with new
          result.Resources[typeName][id] = MergeObjects(existingData, newData);
        }
        else
        {
          // Add new resource
          result.Resources[typeName][id] = (JObject)newData.DeepClone();
        }
      }
    }

    return result;
  }

  /// <summary>
  /// Deep merge two JObjects.
  /// Properties in overlay replace/extend base.
  /// - Primitives: overlay replaces base
  /// - Objects: recursively merge
  /// - Arrays: overlay replaces base
  /// </summary>
  private JObject MergeObjects(JObject baseObj, JObject overlay)
  {
    var result = (JObject)baseObj.DeepClone();

    foreach (var prop in overlay.Properties())
    {
      if (result[prop.Name] is JObject existingObj && prop.Value is JObject overlayObj)
      {
        // Recursively merge nested objects
        result[prop.Name] = MergeObjects(existingObj, overlayObj);
      }
      else
      {
        // Replace (primitives, arrays, or new properties)
        result[prop.Name] = prop.Value.DeepClone();
      }
    }

    return result;
  }
}
