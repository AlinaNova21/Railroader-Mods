using Track;
using UnityEngine;

namespace AlinasRailTools.Scripting.Turntables;

/// <summary>
/// MonoBehaviour component that represents a turntable instance
/// This component contains only data and identification - build logic is in TurntableInstanceBuilder
/// </summary>
public class TurntableInstance : MonoBehaviour
{
  public string identifier;
  public int radius;
  public int subdivisions;
  public PrefabInstance prefabInstance; // Direct reference to prefab instance
  public Turntable turntable; // Reference to the Track.Turntable component created during build
}
