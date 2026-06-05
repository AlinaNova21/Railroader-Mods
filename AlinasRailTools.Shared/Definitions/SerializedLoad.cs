using Newtonsoft.Json;

namespace AlinasRailTools.Shared.Definitions;

public enum LoadUnit
{
  Pounds,
  Gallons,
  CubicFeet,
  Tons,
  Items,
  Quantity
}

public class SerializedLoad
{
  [JsonProperty("id")]
  public string Id { get; set; } = "";

  [JsonProperty("description")]
  public string Description { get; set; } = "";

  [JsonProperty("units")]
  public LoadUnit Units { get; set; } = LoadUnit.Pounds;

  [JsonProperty("density")]
  public float Density { get; set; } = 30.0f;

  [JsonProperty("unitWeightInPounds")]
  public float UnitWeightInPounds { get; set; } = 0.0f;

  [JsonProperty("importable")]
  public bool Importable { get; set; } = true;

  [JsonProperty("payPerQuantity")]
  public float PayPerQuantity { get; set; } = 0.0f;

  [JsonProperty("costPerUnit")]
  public float CostPerUnit { get; set; } = 0.0f;
}
