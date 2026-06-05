using System;
using System.Collections.Generic;
using System.Linq;
using AlinasRailTools.Scripting.Ops;
using Helpers;
using UnityEngine;

namespace AlinasRailTools.Scripting;

public class SceneryBuilder(Helper helper)
{
  public Helper Helper => helper;
  public Helper Done() => helper;

  public static Dictionary<string, SceneryAssetInstance> AllScenery = [];
  public static Dictionary<string, SceneryAssetInstance> SceneryCache = [];

  public static void ReloadCaches()
  {
    AllScenery.Clear();
    UnityEngine.Object
      .FindObjectsByType<SceneryAssetInstance>(FindObjectsInactive.Include, FindObjectsSortMode.None)
      .ToList()
      .ForEach(s => AllScenery[s.identifier] = s);

    SceneryCache = AllScenery
      .Where(s => s.Value.gameObject.activeInHierarchy)
      .ToDictionary(k => k.Key, v => v.Value);
  }

  private GameObject GetSceneryParent()
  {
    var world = GameObject.Find("World");
    if (world == null)
    {
      world = new GameObject("World");
    }

    var sceneryParent = world.transform.Find("Scenery")?.gameObject;
    if (sceneryParent == null)
    {
      sceneryParent = new GameObject("Scenery");
      sceneryParent.transform.SetParent(world.transform);
    }

    return sceneryParent;
  }

  public SceneryBuilder GetScenery(string id, Action<SceneryAssetInstanceBuilder> action)
  {
    action(GetScenery(id));
    return this;
  }

  public SceneryAssetInstanceBuilder GetScenery(string id) =>
    SceneryCache.ContainsKey(id) ?
      new SceneryAssetInstanceBuilder(this, SceneryCache[id]) :
      throw new Exception($"Scenery with id {id} does not exist");

  public SceneryBuilder CreateScenery(string id, Action<SceneryAssetInstanceBuilder> action)
  {
    action(CreateScenery(id));
    return this;
  }

  public SceneryAssetInstanceBuilder CreateScenery(string id)
  {
    SceneryAssetInstance scenery;
    if (!AllScenery.TryGetValue(id, out scenery))
    {
      var go = new GameObject($"Scenery {id}");
      go.SetActive(false);
      scenery = go.AddComponent<SceneryAssetInstance>();
      scenery.identifier = id;

      // Parent under World/Scenery
      var sceneryParent = GetSceneryParent();
      scenery.transform.SetParent(sceneryParent.transform);
    }

    // Don't activate by default - let user call Done(true) if they want activation
    AllScenery[id] = scenery;
    if (scenery.gameObject.activeInHierarchy)
    {
      SceneryCache[id] = scenery;
    }
    return new SceneryAssetInstanceBuilder(this, scenery);
  }

  public SceneryBuilder Scenery(string id, Action<SceneryAssetInstanceBuilder> action)
  {
    action(GetOrCreateScenery(id));
    return this;
  }

  public SceneryAssetInstanceBuilder Scenery(string id) => GetOrCreateScenery(id);

  public SceneryBuilder GetOrCreateScenery(string id, Action<SceneryAssetInstanceBuilder> action)
  {
    action(GetOrCreateScenery(id));
    return this;
  }

  public SceneryAssetInstanceBuilder GetOrCreateScenery(string id) =>
    SceneryCache.ContainsKey(id) ? GetScenery(id) : CreateScenery(id);

  public SceneryBuilder RemoveScenery(string id)
  {
    if (SceneryCache.ContainsKey(id))
    {
      SceneryCache[id].gameObject.SetActive(false);
      SceneryCache.Remove(id);
    }
    return this;
  }
}