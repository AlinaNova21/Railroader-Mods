using UI.Map;

namespace AlinasMapMod.Definitions;
public class SerializedMapLabel
{
  public SerializedVector3 Position { get; set; } = new SerializedVector3();
  public string Text { get; set; } = "Map Label";

  public MapLabel.Alignment Alingnment { get; set; } = MapLabel.Alignment.TopLeft;
}
