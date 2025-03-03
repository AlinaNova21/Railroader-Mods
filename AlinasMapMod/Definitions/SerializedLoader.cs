using Track;

namespace AlinasMapMod.Definitions;
public class SerializedLoader
{
  public SerializedVector3 Position { get; set; }
  public SerializedVector3 Rotation { get; set; }
  public string Prefab { get; set; } = "empty://";
  public string Industry { get; set; } = "";
}
