namespace AlinasMapMod.Definitions
{
  public partial class SerializedTurntable
  {
    public string Identifier { get; set; } = "";

    public int Radius { get; set; } = 15;
    public int Subdivisions { get; set; } = 32;
    public SerializedVector3 Position { get; set; }
    public SerializedVector3 Rotation { get; set; }

    public int RoundhouseStalls { get; set; } = 0;
    public int RoundhouseTrackLength { get; set; } = 46;

    public string StallPrefab { get; set; } = "vanilla";
    public string StartPrefab { get; set; } = "vanilla";
    public string EndPrefab { get; set; } = "vanilla";

    public SerializedTurntable()
    {
      Position = new SerializedVector3();
      Rotation = new SerializedVector3();
    }
  }
}
