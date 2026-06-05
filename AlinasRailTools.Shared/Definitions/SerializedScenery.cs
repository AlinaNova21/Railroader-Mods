using Newtonsoft.Json;

namespace AlinasRailTools.Shared.Definitions;

public class SerializedScenery
{
  [JsonProperty("modelIdentifier")]
  public string ModelIdentifier { get; set; } = "";

  [JsonProperty("position")]
  public SerializedVector3 Position { get; set; } = new SerializedVector3();

  [JsonProperty("rotation")]
  public SerializedVector3 Rotation { get; set; } = new SerializedVector3();

  [JsonProperty("scale")]
  public SerializedVector3 Scale { get; set; } = new SerializedVector3 { X = 1, Y = 1, Z = 1 };
}
