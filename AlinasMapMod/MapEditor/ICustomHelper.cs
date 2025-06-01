using UnityEngine;

namespace AlinasMapMod.MapEditor;

public interface ICustomHelper
{
  /// <summary>
  /// Must include a collider on the clickable layer
  /// </summary>
  GameObject HelperPrefab { get; }
}
