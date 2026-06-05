using AlinasRailTools.Shared.Definitions;
using UnityEngine;

namespace AlinasRailTools.Extensions;

public static class VectorExtensions
{
  // Vector3 extensions
  public static SerializedVector3 ToSerialized(this Vector3 vector)
  {
    return new SerializedVector3
    {
      X = vector.x,
      Y = vector.y,
      Z = vector.z
    };
  }

  public static Vector3 ToUnity(this SerializedVector3 serialized)
  {
    return new Vector3(serialized.X, serialized.Y, serialized.Z);
  }

  // Vector2 extensions
  public static SerializedVector2 ToSerialized(this Vector2 vector)
  {
    return new SerializedVector2
    {
      X = vector.x,
      Y = vector.y
    };
  }

  public static Vector2 ToUnity(this SerializedVector2 serialized)
  {
    return new Vector2(serialized.X, serialized.Y);
  }
}
