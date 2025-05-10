using Track;

namespace AlinasMapMod.Definitions;
public class SerializedStationAgent
{
  public SerializedVector3 Position { get; set; }
  public SerializedVector3 Rotation { get; set; }
  public string Prefab { get; set; } = "empty://";
  public string PassengerStop { get; set; } = "";
}
