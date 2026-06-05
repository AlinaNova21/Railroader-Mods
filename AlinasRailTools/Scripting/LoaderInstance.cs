using System.Linq;
using Model.Ops;
using UnityEngine;

namespace AlinasRailTools.Scripting;

/// <summary>
/// Loader component - stores references to prefab instance and industry
/// </summary>
public class LoaderInstance : MonoBehaviour
{
  public string identifier;
  public PrefabInstance prefabInstance;
  public Industry industry;

  public static LoaderInstance FindById(string identifier) => GameObject
    .FindObjectsByType<LoaderInstance>(FindObjectsInactive.Include, FindObjectsSortMode.None)
    .FirstOrDefault(l => l.identifier == identifier);
}
