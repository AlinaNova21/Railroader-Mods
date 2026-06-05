using Newtonsoft.Json;

namespace AlinasRailTools.Shared.Definitions;

public class SerializedTelegraphPoles
{
  [JsonProperty("polesToRaise")]
  public int[] PolesToRaise { get; set; }

  [JsonProperty("polesToMove")]
  public int[] PolesToMove { get; set; }

  [JsonProperty("poleMovement")]
  public float[,] PoleMovement { get; set; }
}
