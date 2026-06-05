using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using Map.Runtime.MaskComponents;
using UnityEngine;

namespace AlinasRailTools.Scripting.Rivers;

public class RiversBuilder(Helper helper)
{
  public Helper Helper => helper;
  public Helper Done() => helper;

  public static Dictionary<string, RiverPath> AllRivers = [];
  public static Dictionary<string, RiverPath> RiversCache = [];

  public static void ReloadCaches()
  {
    AllRivers.Clear();
    UnityEngine.Object
      .FindObjectsByType<RiverPath>(FindObjectsInactive.Include, FindObjectsSortMode.None)
      .ToList()
      .ForEach(r => AllRivers[r.gameObject.name] = r);

    RiversCache = AllRivers
      .Where(r => r.Value.gameObject.activeInHierarchy)
      .ToDictionary(k => k.Key, v => v.Value);
  }

  private GameObject GetRiversParent()
  {
    var world = GameObject.Find("World");
    if (world == null)
    {
      world = new GameObject("World");
    }

    var riversParent = world.transform.Find("Rivers")?.gameObject;
    if (riversParent == null)
    {
      riversParent = new GameObject("Rivers");
      riversParent.transform.SetParent(world.transform);
    }

    return riversParent;
  }

  public RiversBuilder GetRiver(string name, Action<RiverPathBuilder> action)
  {
    action(GetRiver(name));
    return this;
  }

  public RiverPathBuilder GetRiver(string name) =>
    RiversCache.ContainsKey(name) ?
      new RiverPathBuilder(this, RiversCache[name].gameObject) :
      throw new Exception($"River with name {name} does not exist");

  public RiversBuilder CreateRiver(string name, Action<RiverPathBuilder> action)
  {
    action(CreateRiver(name));
    return this;
  }

  public RiverPathBuilder CreateRiver(string name)
  {
    RiverPath riverPath;
    if (!AllRivers.TryGetValue(name, out riverPath))
    {
      var go = new GameObject(name);
      go.SetActive(false);
      riverPath = go.AddComponent<RiverPath>();
      var riverBuilder = go.AddComponent<RiverBuilder>();

      // Parent under World/Rivers
      var riversParent = GetRiversParent();
      go.transform.SetParent(riversParent.transform);
    }

    // Don't activate by default - let user call SetActive(true) if they want activation
    AllRivers[name] = riverPath;
    if (riverPath.gameObject.activeInHierarchy)
    {
      RiversCache[name] = riverPath;
    }
    return new RiverPathBuilder(this, riverPath.gameObject);
  }

  public RiversBuilder River(string name, Action<RiverPathBuilder> action)
  {
    action(GetOrCreateRiver(name));
    return this;
  }

  public RiverPathBuilder River(string name) => GetOrCreateRiver(name);

  public RiversBuilder GetOrCreateRiver(string name, Action<RiverPathBuilder> action)
  {
    action(GetOrCreateRiver(name));
    return this;
  }

  public RiverPathBuilder GetOrCreateRiver(string name) =>
    RiversCache.ContainsKey(name) ? GetRiver(name) : CreateRiver(name);

  public RiversBuilder RemoveRiver(string name)
  {
    if (RiversCache.ContainsKey(name))
    {
      RiversCache[name].gameObject.SetActive(false);
      RiversCache.Remove(name);
    }
    return this;
  }
}
