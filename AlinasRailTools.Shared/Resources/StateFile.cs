using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AlinasRailTools.Shared.Resources;

/// <summary>
/// Represents a state file containing resources organized by type (camelCase keys)
/// Format: { "trackNodes": { "id": {...} }, "scenery": { "id": {...} } }
/// </summary>
public class StateFile
{
  /// <summary>
  /// Resources organized by type: { "trackNodes": { "id": {...} } }
  /// null values indicate deletion markers
  /// </summary>
  public Dictionary<string, Dictionary<string, JObject>> Resources { get; set; }
    = new Dictionary<string, Dictionary<string, JObject>>();

  /// <summary>
  /// Get resources for a specific type (readonly view)
  /// </summary>
  public IReadOnlyDictionary<string, JObject> GetResources(string typeName)
  {
    return Resources.TryGetValue(typeName, out var dict)
      ? dict
      : new Dictionary<string, JObject>();
  }

  /// <summary>
  /// Set resources for a type (removes type if empty)
  /// </summary>
  public void SetResources(string typeName, Dictionary<string, JObject> resources)
  {
    if (resources == null || resources.Count == 0)
    {
      Resources.Remove(typeName);
    }
    else
    {
      Resources[typeName] = resources;
    }
  }

  /// <summary>
  /// Load from JSON string
  /// JsonSubTypes handles polymorphism automatically via discriminator fields
  /// </summary>
  public static StateFile FromJson(string json)
  {
    var jobj = JObject.Parse(json);
    var state = new StateFile();

    foreach (var prop in jobj.Properties())
    {
      if (prop.Value is JObject resources)
      {
        state.Resources[prop.Name] = new Dictionary<string, JObject>();

        foreach (var resProp in resources.Properties())
        {
          if (resProp.Value is JObject resData)
          {
            state.Resources[prop.Name][resProp.Name] = resData;
          }
          else if (resProp.Value.Type == JTokenType.Null)
          {
            // Null = explicit deletion marker
            state.Resources[prop.Name][resProp.Name] = null;
          }
        }
      }
    }

    return state;
  }

  /// <summary>
  /// Save to JSON string with indentation
  /// </summary>
  public string ToJson()
  {
    var jobj = new JObject();

    foreach (var typeEntry in Resources)
    {
      var typeName = typeEntry.Key;
      var resources = typeEntry.Value;
      var typeObj = new JObject();

      foreach (var resourceEntry in resources)
      {
        var id = resourceEntry.Key;
        var data = resourceEntry.Value;
        typeObj[id] = data; // Can be null for deletions
      }

      jobj[typeName] = typeObj;
    }

    return jobj.ToString(Formatting.Indented);
  }

  /// <summary>
  /// Deep clone this state file
  /// </summary>
  public StateFile Clone()
  {
    var clone = new StateFile();

    foreach (var typeEntry in Resources)
    {
      var typeName = typeEntry.Key;
      var resources = typeEntry.Value;
      clone.Resources[typeName] = new Dictionary<string, JObject>();

      foreach (var resourceEntry in resources)
      {
        var id = resourceEntry.Key;
        var data = resourceEntry.Value;
        clone.Resources[typeName][id] = data != null
          ? (JObject)data.DeepClone()
          : null;
      }
    }

    return clone;
  }
}
