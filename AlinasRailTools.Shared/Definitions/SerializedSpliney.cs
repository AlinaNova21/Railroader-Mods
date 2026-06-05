using System.Collections.Generic;
using JsonSubTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AlinasRailTools.Shared.Definitions;

/// <summary>
/// Represents a dynamic spliney object. Splineys use a handler field to determine
/// their type, and can have arbitrary additional properties based on the handler type.
/// Use ExtensionData to capture all unrecognized properties for proper round-tripping.
/// Uses JsonSubTypes for discriminator-based polymorphic deserialization.
/// </summary>
[JsonConverter(typeof(JsonSubtypes), "handler")]
[JsonSubtypes.KnownSubType(typeof(SerializedTurntableSpliney), "AlinasMapMod.Turntable.TurntableBuilder")]
[JsonSubtypes.KnownSubType(typeof(SerializedLoaderSpliney), "AlinasMapMod.Loaders.LoaderBuilder")]
[JsonSubtypes.KnownSubType(typeof(SerializedStationAgentSpliney), "AlinasMapMod.Stations.StationAgentBuilder")]
[JsonSubtypes.KnownSubType(typeof(SerializedMapLabelSpliney), "AlinasMapMod.Map.MapLabelBuilder")]
[JsonSubtypes.KnownSubType(typeof(SerializedTelegraphPoleSpliney), "AlinasMapMod.TelegraphPoles.TelegraphPoleBuilder")]
public class SerializedSpliney
{
  [JsonProperty("id")]
  public string Id { get; set; } = "";

  /// <summary>
  /// The fully-qualified class name of the builder (e.g., "AlinasMapMod.Turntable.TurntableBuilder")
  /// </summary>
  [JsonProperty("handler")]
  public string Handler { get; set; } = "";

  /// <summary>
  /// Captures all additional properties not explicitly defined.
  /// Different handler types require different properties.
  /// </summary>
  [JsonExtensionData]
  public Dictionary<string, JToken> AdditionalData { get; set; } = new Dictionary<string, JToken>();
}
