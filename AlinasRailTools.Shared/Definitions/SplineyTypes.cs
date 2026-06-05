using Newtonsoft.Json;

namespace AlinasRailTools.Shared.Definitions;

/// <summary>
/// Turntable spliney with strongly-typed properties
/// </summary>
public class SerializedTurntableSpliney : SerializedSpliney
{
  [JsonProperty("position")]
  public SerializedVector3 Position { get; set; } = new SerializedVector3();

  [JsonProperty("rotation")]
  public SerializedVector3 Rotation { get; set; } = new SerializedVector3();

  [JsonProperty("radius")]
  public int Radius { get; set; } = 15;

  [JsonProperty("subdivisions")]
  public int Subdivisions { get; set; } = 32;

  [JsonProperty("roundhouseStalls")]
  public int RoundhouseStalls { get; set; } = 0;

  [JsonProperty("roundhouseTrackLength")]
  public int RoundhouseTrackLength { get; set; } = 46;

  [JsonProperty("stallPrefab")]
  public string StallPrefab { get; set; } = "vanilla://roundhouseStall";

  [JsonProperty("startPrefab")]
  public string StartPrefab { get; set; } = "vanilla://roundhouseStart";

  [JsonProperty("endPrefab")]
  public string EndPrefab { get; set; } = "vanilla://roundhouseEnd";
}

/// <summary>
/// Loader spliney with strongly-typed properties
/// </summary>
public class SerializedLoaderSpliney : SerializedSpliney
{
  [JsonProperty("position")]
  public SerializedVector3 Position { get; set; } = new SerializedVector3();

  [JsonProperty("rotation")]
  public SerializedVector3 Rotation { get; set; } = new SerializedVector3();

  [JsonProperty("prefab")]
  public string Prefab { get; set; } = "empty://";

  [JsonProperty("industry")]
  public string Industry { get; set; } = "";
}

/// <summary>
/// Station agent spliney with strongly-typed properties
/// </summary>
public class SerializedStationAgentSpliney : SerializedSpliney
{
  [JsonProperty("position")]
  public SerializedVector3 Position { get; set; } = new SerializedVector3();

  [JsonProperty("rotation")]
  public SerializedVector3 Rotation { get; set; } = new SerializedVector3();

  [JsonProperty("prefab")]
  public string Prefab { get; set; } = "empty://";

  [JsonProperty("passengerStop")]
  public string PassengerStop { get; set; } = "whittier";
}

/// <summary>
/// Map label spliney with strongly-typed properties
/// </summary>
public class SerializedMapLabelSpliney : SerializedSpliney
{
  [JsonProperty("position")]
  public SerializedVector3 Position { get; set; } = new SerializedVector3();

  [JsonProperty("text")]
  public string Text { get; set; } = "Map Label";
}

/// <summary>
/// Telegraph pole spliney with strongly-typed properties
/// </summary>
public class SerializedTelegraphPoleSpliney : SerializedSpliney
{
  [JsonProperty("polesToRaise")]
  public int[] PolesToRaise { get; set; } = [];

  [JsonProperty("polesToMove")]
  public int[] PolesToMove { get; set; } = [];

  [JsonProperty("poleMovement")]
  public float[,] PoleMovement { get; set; }
}
