using System;
using System.Collections.Generic;
using UnityEngine;

namespace AlinasMapMod;
internal static class VanillaPrefabs
{
  private static Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();

  public static string[] AvailablePrefabs => [
    .. AvailableRoundhousePrefabs,
    .. AvailableLoaderPrefabs,
    .. AvailableStationPrefabs
  ];

  public static string[] AvailableRoundhousePrefabs => [
    "roundhouseStall",
    "roundhouseStart",
    "roundhouseEnd"
  ];

  public static string[] AvailableLoaderPrefabs => [
    "coalConveyor",
    "coalTower",
    "dieselFuelingStand",
    "waterTower",
    "waterColumn"
  ];

  public static string[] AvailableStationPrefabs => [
    "flagStopStation",
    "brysonDepot",
    "dillsboroDepot",
    "southernCombinationDepot",
  ];

  public static GameObject GetPrefab(string key)
  {
    if (!prefabs.ContainsKey(key)) {
      var prefab = key switch
      {
        "roundhouseStall" => GenerateRoundhouseStallPrefab(),
        "roundhouseStart" => GenerateRoundhouseEndPrefab(false),
        "roundhouseEnd" => GenerateRoundhouseEndPrefab(true),
        "coalConveyor" => Clone(Utils.GameObjectFromUri("path://scene/World/Large Scenery/Whittier/Coal Conveyor")),
        "coalTower" => Clone(Utils.GameObjectFromUri("path://scene/World/Large Scenery/Bryson/Bryson Coaling Tower")),
        "dieselFuelingStand" => Clone(Utils.GameObjectFromUri("path://scene/World/Large Scenery/Whittier/East Whittier Diesel Fueling Stand")),
        "waterTower" => Clone(Utils.GameObjectFromUri("path://scene/World/Large Scenery/Whittier Water Tower")),
        "waterColumn" => Clone(Utils.GameObjectFromUri("path://scene/World/Large Scenery/Whittier/Water Column")),
        "flagStopStation" => Clone(Utils.GameObjectFromUri("path://scene/World/Large Scenery/Ela/flagstopstation")),
        "brysonDepot" => Clone(Utils.GameObjectFromUri("path://scene/World/Large Scenery/Bryson/Bryson Depot")),
        "dillsboroDepot" => Clone(Utils.GameObjectFromUri("path://scene/World/Large Scenery/Dillsboro/Dillsboro Depot")),
        "southernCombinationDepot" => Clone(Utils.GameObjectFromUri("path://scene/World/Large Scenery/Whittier/Southern Combination Depot")),
        _ => throw new ArgumentException("Attempted to load unknown vanilla prefab: " + key),
      };
      prefabs.Add(key, prefab);
    }
    return prefabs[key];
  }

  public static void ClearCache()
  {
    foreach (var prefab in prefabs.Values) {
      GameObject.Destroy(prefab);
    }
    prefabs.Clear();
  }

  private static GameObject Clone(GameObject go)
  {
    var parent = GameObject.Find("AlinasMapMod")?.transform;
    var ngo = GameObject.Instantiate(go, parent);
    ngo.SetActive(false);
    ngo.transform.localPosition = Vector3.zero;
    ngo.transform.localEulerAngles = Vector3.zero;
    return ngo;
  }

  private static GameObject GenerateRoundhouseStallPrefab()
  {
    var go = new GameObject("Roundhouse Stall");
    var stallPrefab = Utils.GameObjectFromUri("path://scene/World/Large Scenery/Bryson/Bryson Turntable/Roundhouse/Stall");
    var betweenPrefab = Utils.GameObjectFromUri("path://scene/World/Large Scenery/Bryson/Bryson Turntable/Roundhouse/Roundhouse Modular A Between");

    var stall = GameObject.Instantiate(stallPrefab, go.transform);
    stall.transform.localPosition = Vector3.zero;
    stall.transform.localEulerAngles = new Vector3(0, 0, 0);
    stall.transform.localScale = new Vector3(1, 1, 1);

    var between = GameObject.Instantiate(betweenPrefab, go.transform);
    between.transform.localPosition = Vector3.zero;
    between.transform.localEulerAngles = new Vector3(270, 180 - (11.25f / 2), 0);

    return go;
  }

  private static GameObject GenerateRoundhouseEndPrefab(bool start)
  {
    var stallPrefab = Utils.GameObjectFromUri("path://scene/World/Large Scenery/Bryson/Bryson Turntable/Roundhouse/Stall");
    var endPrefab = Utils.GameObjectFromUri("path://scene/World/Large Scenery/Bryson/Bryson Turntable/Roundhouse/Roundhouse Modular A Side");
    var betweenPrefab = Utils.GameObjectFromUri("path://scene/World/Large Scenery/Bryson/Bryson Turntable/Roundhouse/Roundhouse Modular A Between");

    var go = new GameObject("Roundhouse End");
    var stall = GameObject.Instantiate(stallPrefab, go.transform);
    stall.transform.localPosition = Vector3.zero;
    stall.transform.localEulerAngles = new Vector3(0, 0, 0);
    stall.transform.localScale = new Vector3(1, 1, 1);

    var end = GameObject.Instantiate(endPrefab, go.transform);
    end.transform.localPosition = Vector3.zero;
    end.transform.localEulerAngles = new Vector3(0, 180, 0);
    end.transform.localScale = new Vector3(start ? 1 : -1, 1, 1);

    if (!start) {
      var between = GameObject.Instantiate(betweenPrefab, go.transform);
      between.transform.localPosition = Vector3.zero;
      between.transform.localEulerAngles = new Vector3(270, 180 + (11.25f / 2), 0);
    }

    return go;
  }
}
