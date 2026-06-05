using Newtonsoft.Json;

namespace AlinasRailTools.Shared.Definitions;

public class SerializedMapLabel
{
  [JsonProperty("position")]
  public SerializedVector3 Position { get; set; } = new SerializedVector3();

  [JsonProperty("text")]
  public string Text { get; set; } = "Map Label";
}
