using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AlinasRailTools.Scripting.Turntables;

public class RoundhouseBuilder(Helper helper)
{
  public Helper Helper => helper;
  public Helper Done() => helper;

  public static Dictionary<string, RoundhouseInstance> AllRoundhouses = [];
  public static Dictionary<string, RoundhouseInstance> RoundhousesCache = [];

  public static void ReloadCaches()
  {
    AllRoundhouses.Clear();
    UnityEngine.Object
      .FindObjectsByType<RoundhouseInstance>(FindObjectsInactive.Include, FindObjectsSortMode.None)
      .ToList()
      .ForEach(r => AllRoundhouses[r.identifier] = r);

    RoundhousesCache = AllRoundhouses
      .Where(r => r.Value.enabled)
      .ToDictionary(k => k.Key, v => v.Value);
  }

  public RoundhouseBuilder GetRoundhouse(string id, Action<RoundhouseInstanceBuilder> action)
  {
    action(GetRoundhouse(id));
    return this;
  }

  public RoundhouseInstanceBuilder GetRoundhouse(string id)
  {
    if (!RoundhousesCache.TryGetValue(id, out var roundhouse))
    {
      throw new Exception($"Roundhouse with id {id} does not exist");
    }
    return new RoundhouseInstanceBuilder(this, roundhouse);
  }

  public RoundhouseBuilder CreateRoundhouse(string id, Action<RoundhouseInstanceBuilder> action)
  {
    action(CreateRoundhouse(id));
    return this;
  }

  public RoundhouseInstanceBuilder CreateRoundhouse(string id)
  {
    RoundhouseInstance roundhouse;
    if (!AllRoundhouses.TryGetValue(id, out roundhouse) || roundhouse == null)
    {
      var go = new GameObject(id);
      go.SetActive(false);
      roundhouse = go.AddComponent<RoundhouseInstance>();
      roundhouse.identifier = id;
      roundhouse.subdivisions = 32; // Default subdivisions
      roundhouse.stalls = 0; // Default no stalls

      // Set default piece templates from vanilla roundhouse
      if (PrefabBuilder.Templates.TryGetValue("roundhouseStall", out var stallTemplate))
      {
        roundhouse.stallTemplate = stallTemplate;
      }
      if (PrefabBuilder.Templates.TryGetValue("roundhouseSide", out var sideTemplate))
      {
        roundhouse.sideTemplate = sideTemplate;
      }
      if (PrefabBuilder.Templates.TryGetValue("roundhouseBetween", out var betweenTemplate))
      {
        roundhouse.betweenTemplate = betweenTemplate;
      }

      // Parent under World/Roundhouses
      var world = GameObject.Find("World");
      if (world == null)
      {
        world = new GameObject("World");
      }
      var roundhousesContainer = world.transform.Find("Roundhouses")?.gameObject;
      if (roundhousesContainer == null)
      {
        roundhousesContainer = new GameObject("Roundhouses");
        roundhousesContainer.transform.SetParent(world.transform);
      }
      go.transform.SetParent(roundhousesContainer.transform);
      // DO NOT activate here - GameObject stays inactive until Build() completes
    }

    roundhouse.enabled = true;
    AllRoundhouses[id] = roundhouse;
    RoundhousesCache[id] = roundhouse;
    return new RoundhouseInstanceBuilder(this, roundhouse);
  }

  public RoundhouseBuilder Roundhouse(string id, Action<RoundhouseInstanceBuilder> action)
  {
    action(GetOrCreateRoundhouse(id));
    return this;
  }

  public RoundhouseInstanceBuilder Roundhouse(string id) => GetOrCreateRoundhouse(id);

  public RoundhouseBuilder GetOrCreateRoundhouse(string id, Action<RoundhouseInstanceBuilder> action)
  {
    action(GetOrCreateRoundhouse(id));
    return this;
  }

  public RoundhouseInstanceBuilder GetOrCreateRoundhouse(string id)
  {
    return RoundhousesCache.ContainsKey(id) && RoundhousesCache[id] != null
      ? GetRoundhouse(id)
      : CreateRoundhouse(id);
  }

  public RoundhouseBuilder RemoveRoundhouse(string id)
  {
    if (RoundhousesCache.ContainsKey(id))
    {
      var roundhouse = RoundhousesCache[id];
      roundhouse.enabled = false;
      RoundhousesCache.Remove(id);
    }
    return this;
  }
}
