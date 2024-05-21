using System.Numerics;

namespace AlinasMapMod.Definitions
{
  public class SerializedTurntable
  {
    public class SerializedVector3
    {
      public float x { get; set; }
      public float y { get; set; }
      public float z { get; set; }
    }
    public string Identifier { get; set; } = "";
    public SerializedVector3 Position { get; set; }
    public SerializedVector3 Rotation { get; set; }

    public int RoundhouseStalls { get; set; } = 0;

    public SerializedTurntable()
    {
      Position = new SerializedVector3();
      Rotation = new SerializedVector3();
    }
  }
}
