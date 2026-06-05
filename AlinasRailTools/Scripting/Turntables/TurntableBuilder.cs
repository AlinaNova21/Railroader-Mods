using System;
using System.Collections.Generic;
using System.Linq;
using Track;
using UnityEngine;

namespace AlinasRailTools.Scripting.Turntables;

public class TurntableBuilder(Helper helper)
{
  public Helper Helper => helper;
  public Helper Done() => helper;

  public static Dictionary<string, TurntableInstance> AllTurntables = [];
  public static Dictionary<string, TurntableInstance> TurntablesCache = [];

  public static void ReloadCaches()
  {
    AllTurntables.Clear();
    UnityEngine.Object
      .FindObjectsByType<TurntableInstance>(FindObjectsInactive.Include, FindObjectsSortMode.None)
      .ToList()
      .ForEach(t => AllTurntables[t.identifier] = t);

    TurntablesCache = AllTurntables
      .Where(t => t.Value.enabled)
      .ToDictionary(k => k.Key, v => v.Value);
  }

  public TurntableBuilder GetTurntable(string id, Action<TurntableInstanceBuilder> action)
  {
    action(GetTurntable(id));
    return this;
  }

  public TurntableInstanceBuilder GetTurntable(string id)
  {
    if (!TurntablesCache.TryGetValue(id, out var turntable))
    {
      throw new Exception($"Turntable with id {id} does not exist");
    }
    return new TurntableInstanceBuilder(this, turntable);
  }

  public TurntableBuilder CreateTurntable(string id, Action<TurntableInstanceBuilder> action)
  {
    action(CreateTurntable(id));
    return this;
  }

  public TurntableInstanceBuilder CreateTurntable(string id)
  {
    TurntableInstance turntable;
    if (!AllTurntables.TryGetValue(id, out turntable) || turntable == null)
    {
      var go = new GameObject(id);
      go.SetActive(false); // Keep inactive - will be activated by SetActive() after Build()
      turntable = go.AddComponent<TurntableInstance>();
      turntable.identifier = id;
      turntable.radius = 15; // Default radius
      turntable.subdivisions = 32; // Default subdivisions

      // Parent under Graph.Shared (Track.Turntable needs to be in the graph hierarchy)
      go.transform.SetParent(Track.Graph.Shared.transform);

      // DO NOT activate here - GameObject stays inactive until Build() completes
    }

    turntable.enabled = true;
    AllTurntables[id] = turntable;
    TurntablesCache[id] = turntable;
    return new TurntableInstanceBuilder(this, turntable);
  }

  public TurntableBuilder Turntable(string id, Action<TurntableInstanceBuilder> action)
  {
    action(GetOrCreateTurntable(id));
    return this;
  }

  public TurntableInstanceBuilder Turntable(string id) => GetOrCreateTurntable(id);

  public TurntableBuilder GetOrCreateTurntable(string id, Action<TurntableInstanceBuilder> action)
  {
    action(GetOrCreateTurntable(id));
    return this;
  }

  public TurntableInstanceBuilder GetOrCreateTurntable(string id)
  {
    return TurntablesCache.ContainsKey(id) && TurntablesCache[id] != null
      ? GetTurntable(id)
      : CreateTurntable(id);
  }

  public TurntableBuilder RemoveTurntable(string id)
  {
    if (TurntablesCache.ContainsKey(id))
    {
      var turntable = TurntablesCache[id];
      turntable.enabled = false;
      TurntablesCache.Remove(id);
    }
    return this;
  }
}
