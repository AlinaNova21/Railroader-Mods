using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AlinasRailTools.Shared.ModLoading;

namespace AlinasRailTools.Shared.Definitions;

/// <summary>
/// Railloader mod Definition.json structure
/// </summary>
public class RLModDefinition
{
  [JsonProperty("manifestVersion")]
  public int ManifestVersion { get; set; } = 5;

  [JsonProperty("id")]
  public string Id { get; set; } = "";

  [JsonProperty("name")]
  public string Name { get; set; } = "";

  [JsonProperty("version")]
  public string Version { get; set; } = "";

  [JsonProperty("assemblies")]
  public string[] Assemblies { get; set; } = [];

  [JsonProperty("updateUrl")]
  public string UpdateUrl { get; set; } = "";

  [JsonProperty("requires")]
  [JsonConverter(typeof(RLModRequirementArrayConverter))]
  public RLModRequirement[] Requires { get; set; } = [];

  [JsonProperty("loadBefore")]
  [JsonConverter(typeof(RLModRequirementArrayConverter))]
  public RLModRequirement[] LoadBefore { get; set; } = [];

  [JsonProperty("loadAfter")]
  [JsonConverter(typeof(RLModRequirementArrayConverter))]
  public RLModRequirement[] LoadAfter { get; set; } = [];

  [JsonProperty("mixintos")]
  public MixintoDefinitions Mixintos { get; set; } = new MixintoDefinitions();

  [JsonProperty("conflictsWith")]
  [JsonConverter(typeof(RLModRequirementArrayConverter))]
  public RLModRequirement[] ConflictsWith { get; set; } = [];
}

/// <summary>
/// Represents a mod requirement - can be a simple string ID or a full object with version constraints
/// </summary>
public class RLModRequirement
{
  [JsonProperty("id")]
  public string Id { get; set; } = "";

  [JsonProperty("notBefore")]
  public string NotBefore { get; set; } = "";

  [JsonProperty("notAfter")]
  public string NotAfter { get; set; } = "";

  /// <summary>
  /// Creates a requirement from a simple mod ID string
  /// </summary>
  public static RLModRequirement FromString(string modId)
  {
    return new RLModRequirement { Id = modId };
  }
}

/// <summary>
/// Converter that handles requires being either strings or objects
/// </summary>
public class RLModRequirementArrayConverter : JsonConverter<RLModRequirement[]>
{
  public override RLModRequirement[] ReadJson(JsonReader reader, Type objectType, RLModRequirement[] existingValue, bool hasExistingValue, JsonSerializer serializer)
  {
    var result = new List<RLModRequirement>();
    var token = JToken.Load(reader);

    if (token.Type == JTokenType.Array)
    {
      foreach (var item in token)
      {
        if (item.Type == JTokenType.String)
        {
          // Simple string: "Zamu.StrangeCustoms"
          result.Add(RLModRequirement.FromString(item.ToString()));
        }
        else if (item.Type == JTokenType.Object)
        {
          // Full object: {"id": "...", "notBefore": "..."}
          var requirement = item.ToObject<RLModRequirement>(serializer);
          if (requirement != null)
          {
            result.Add(requirement);
          }
        }
      }
    }

    return result.ToArray();
  }

  public override void WriteJson(JsonWriter writer, RLModRequirement[] value, JsonSerializer serializer)
  {
    // Always write as full object array for consistency
    var array = new JArray();
    foreach (var requirement in value)
    {
      array.Add(JToken.FromObject(requirement, serializer));
    }
    array.WriteTo(writer);
  }
}
