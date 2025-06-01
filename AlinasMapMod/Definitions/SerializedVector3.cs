using AlinasMapMod.Definitions.Converters;
using Newtonsoft.Json;
using UnityEngine;

namespace AlinasMapMod.Definitions;

[JsonConverter(typeof(InlineJsonConverter))]
public class SerializedVector3
{
  public float x { get; set; }
  public float y { get; set; }
  public float z { get; set; }

  public static implicit operator Vector3(SerializedVector3 vector)
  {
    return new Vector3(vector.x, vector.y, vector.z);
  }

  public static implicit operator SerializedVector3(Vector3 vector)
  {
    return new SerializedVector3
    {
      x = vector.x,
      y = vector.y,
      z = vector.z
    };
  }
}
