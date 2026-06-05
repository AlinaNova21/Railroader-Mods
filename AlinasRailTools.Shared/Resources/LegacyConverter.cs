using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace AlinasRailTools.Shared.Resources;

/// <summary>
/// Converts legacy game-graph format to new StateFile format
/// Maps snake-case keys to camelCase and handles null deletions
/// JsonSubTypes handles polymorphism automatically via discriminator fields
/// </summary>
public class LegacyConverter
{
  private static readonly Dictionary<string, string> KeyMapping = new()
  {
    ["tracks.nodes"] = "trackNodes",
    ["tracks.segments"] = "trackSegments",
    ["tracks.spans"] = "trackSpans",
    ["scenery"] = "scenery",
    ["areas"] = "areas",
    ["progressions"] = "progressions",
  };

  /// <summary>
  /// Convert legacy game-graph JSON to StateFile
  /// JsonSubTypes automatically handles polymorphism via "type" and "handler" discriminators
  /// </summary>
  public StateFile ConvertToStateFile(JObject legacyJson)
  {
    var state = new StateFile();

    foreach (var mapping in KeyMapping)
    {
      var oldPath = mapping.Key;
      var newType = mapping.Value;
      var token = legacyJson.SelectToken(oldPath);

      if (token is JObject obj)
      {
        state.Resources[newType] = new Dictionary<string, JObject>();

        foreach (var prop in obj.Properties())
        {
          if (prop.Value.Type == JTokenType.Null)
          {
            // null = deletion in legacy format
            state.Resources[newType][prop.Name] = null;
          }
          else if (prop.Value is JObject resourceData)
          {
            // Remap PassengerStop from old AlinasMapMod namespace to base game namespace
            RemapPassengerStopType(resourceData);

            state.Resources[newType][prop.Name] = resourceData;
          }
        }
      }
    }

    return state;
  }

  /// <summary>
  /// Remap PassengerStop type from old AlinasMapMod namespace to base game namespace
  /// PassengerStop was originally added by AlinasMapMod but is now a base game type
  /// </summary>
  private void RemapPassengerStopType(JObject resourceData)
  {
    // Handle both array format (AlinasMapMod) and object format (game dump)
    if (resourceData["components"] is JArray componentsArray)
    {
      foreach (var component in componentsArray)
      {
        if (component is JObject componentObj)
        {
          RemapComponentType(componentObj);
        }
      }
    }
    else if (resourceData["components"] is JObject componentsObject)
    {
      foreach (var componentEntry in componentsObject)
      {
        if (componentEntry.Value is JObject componentObj)
        {
          RemapComponentType(componentObj);
        }
      }
    }

    // Also check nested industries (for game dump format: areas > industries > components)
    if (resourceData["industries"] is JObject industries)
    {
      foreach (var industryEntry in industries)
      {
        if (industryEntry.Value is JObject industryObj)
        {
          RemapPassengerStopType(industryObj); // Recursive call
        }
      }
    }
  }

  private void RemapComponentType(JObject componentObj)
  {
    var typeToken = componentObj["type"];
    if (typeToken?.Type == JTokenType.String &&
        typeToken.Value<string>() == "AlinasMapMod.Ops.PassengerStop")
    {
      componentObj["type"] = "Model.Ops.PassengerStop";
    }
  }
}
