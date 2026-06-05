using Newtonsoft.Json;

namespace AlinasRailTools.Shared.Definitions;

public class SerializedStationAgent
{
  [JsonProperty("position")]
  public SerializedVector3 Position { get; set; }

  [JsonProperty("rotation")]
  public SerializedVector3 Rotation { get; set; }

  [JsonProperty("prefab")]
  public string Prefab { get; set; } = "empty://";

  [JsonProperty("passengerStop")]
  public string PassengerStop { get; set; } = "whittier";
}
