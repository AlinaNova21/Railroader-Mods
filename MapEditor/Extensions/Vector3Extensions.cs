using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MapEditor.Extensions;

public static class Vector3Extensions
{

  public static JObject ToJObject(this Vector3 vector3)
  {
    return new JObject
    {
      { "x", vector3.x },
      { "y", vector3.y },
      { "z", vector3.z },
    };
  }

}
