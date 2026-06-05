using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AlinasRailTools.Shared.ModLoading;

/// <summary>
/// Represents a mixinto reference
/// Format: "file(path.json)"
/// </summary>
public class MixintoReference
{
  /// <summary>
  /// The file path (without the "file(...)" wrapper)
  /// </summary>
  public string File { get; set; }

  /// <summary>
  /// Creates a mixinto reference from a string (e.g., "file(path.json)")
  /// </summary>
  public static MixintoReference FromString(string value)
  {
    var file = value;

    // Parse "file(path.json)" format
    if (value.StartsWith("file(") && value.EndsWith(")"))
    {
      file = value.Substring(5, value.Length - 6);
    }

    return new MixintoReference
    {
      File = file
    };
  }

  public override string ToString()
  {
    return $"file({File})";
  }
}

/// <summary>
/// Custom converter that handles mixinto references as strings
/// </summary>
public class MixintoReferenceConverter : JsonConverter<MixintoReference>
{
  public override MixintoReference ReadJson(JsonReader reader, Type objectType, MixintoReference existingValue, bool hasExistingValue, JsonSerializer serializer)
  {
    var token = JToken.Load(reader);

    if (token.Type == JTokenType.String)
    {
      // String format: "file(path.json)"
      return MixintoReference.FromString(token.ToString());
    }

    return null;
  }

  public override void WriteJson(JsonWriter writer, MixintoReference value, JsonSerializer serializer)
  {
    // Write as string: "file(path.json)"
    writer.WriteValue($"file({value.File})");
  }
}

/// <summary>
/// Mixinto definitions - a flat dictionary where keys indicate type (e.g., "game-graph", "progressions", "container:name")
/// Values can be:
/// - A single string: "file(path.json)"
/// - An array of strings: ["file(path1.json)", "file(path2.json)"]
/// - An array of objects: [{"path": "file(path.json)"}]
/// </summary>
[JsonConverter(typeof(MixintoDefinitionsConverter))]
public class MixintoDefinitions : Dictionary<string, List<MixintoReference>>
{
  /// <summary>
  /// Gets all mixintos of a specific type prefix (e.g., "game-graph")
  /// </summary>
  public IEnumerable<MixintoReference> GetByType(string typePrefix)
  {
    return this.Where(kvp => kvp.Key.StartsWith(typePrefix))
               .SelectMany(kvp => kvp.Value);
  }

  /// <summary>
  /// Gets all game-graph mixintos (exact key match only)
  /// </summary>
  public IEnumerable<MixintoReference> GetGameGraphs()
  {
    return this.TryGetValue("game-graph", out var references)
      ? references
      : Enumerable.Empty<MixintoReference>();
  }

  /// <summary>
  /// Gets all progression mixintos (exact key match only)
  /// </summary>
  public IEnumerable<MixintoReference> GetProgressions()
  {
    return this.TryGetValue("progressions", out var references)
      ? references
      : Enumerable.Empty<MixintoReference>();
  }

  /// <summary>
  /// Gets all container mixintos with their container names
  /// </summary>
  public IEnumerable<(string ContainerName, MixintoReference Reference)> GetContainers()
  {
    return this.Where(kvp => kvp.Key.StartsWith("container:"))
               .SelectMany(kvp => kvp.Value.Select(v => (kvp.Key.Substring(10), v)));
  }
}

/// <summary>
/// Custom converter for MixintoDefinitions to handle string, array of strings, or array of objects
/// </summary>
public class MixintoDefinitionsConverter : JsonConverter<MixintoDefinitions>
{
  public override MixintoDefinitions ReadJson(JsonReader reader, Type objectType, MixintoDefinitions existingValue, bool hasExistingValue, JsonSerializer serializer)
  {
    var result = new MixintoDefinitions();
    var obj = JObject.Load(reader);

    foreach (var prop in obj.Properties())
    {
      var references = new List<MixintoReference>();
      var token = prop.Value;

      if (token.Type == JTokenType.String)
      {
        // Single string: "file(path.json)"
        references.Add(MixintoReference.FromString(token.ToString()));
      }
      else if (token.Type == JTokenType.Array)
      {
        // Array of strings or objects: ["file(path.json)", "file(path2.json)"]
        foreach (var item in token)
        {
          if (item.Type == JTokenType.String)
          {
            references.Add(MixintoReference.FromString(item.ToString()));
          }
          else if (item.Type == JTokenType.Object)
          {
            // Object format: {"path": "file(path.json)"}
            var reference = item.ToObject<MixintoReference>(serializer);
            if (reference != null)
            {
              references.Add(reference);
            }
          }
        }
      }

      result[prop.Name] = references;
    }

    return result;
  }

  public override void WriteJson(JsonWriter writer, MixintoDefinitions value, JsonSerializer serializer)
  {
    // Always write as full structure for consistency
    var obj = new JObject();

    foreach (var kvp in value)
    {
      var array = new JArray();
      foreach (var reference in kvp.Value)
      {
        array.Add($"file({reference.File})");
      }
      obj[kvp.Key] = array;
    }

    obj.WriteTo(writer);
  }
}
