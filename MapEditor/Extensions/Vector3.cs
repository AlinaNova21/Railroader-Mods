using UnityEngine;

namespace MapEditor.Extensions
{
  public static class Vector3Extensions
  {

    public static Vector3 Clone(this Vector3 vector)
    {
      return new Vector3(vector.x, vector.y, vector.z);
    }

  }
}
