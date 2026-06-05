using Newtonsoft.Json;

namespace AlinasRailTools.Shared.Definitions;

public class SerializedTrackNode
{
  [JsonProperty("position")]
  public SerializedVector3 Position { get; set; }

  [JsonProperty("rotation")]
  public SerializedVector3 Rotation { get; set; }

  [JsonProperty("flipSwitchStand")]
  public bool FlipSwitchStand { get; set; } = false;
}
