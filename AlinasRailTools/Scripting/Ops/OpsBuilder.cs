using System;
using System.Collections.Generic;
using System.Linq;
using Model;
using Model.Ops;
using Model.Ops.Definition;
using UnityEngine;

namespace AlinasRailTools.Scripting.Ops;

public class OpsBuilder(Helper helper)
{
  public Helper Helper => helper;
  public Helper Done() => helper;

  public static Dictionary<string, Area> AllAreas = [];
  public static Dictionary<string, Area> Areas = [];

  public static Dictionary<string, Load> AllLoads = [];
  public static Dictionary<string, Load> Loads = [];

  public static void ReloadCaches()
  {
    AllAreas.Clear();
    UnityEngine.Object
      .FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None)
      .ToList()
      .ForEach(a => AllAreas[a.identifier] = a);
    Areas = AllAreas
      .Where(a => a.Value.enabled)
      .ToDictionary(k => k.Key, v => v.Value);

    AllLoads.Clear();
    TrainController.Shared.carPrototypeLibrary.opsLoads
      .ToList()
      .ForEach(l => AllLoads[l.name] = l);
    Loads = AllLoads;
  }

  public OpsBuilder GetArea(string id, Action<AreaBuilder> action) { action(GetArea(id)); return this; }
  public AreaBuilder GetArea(string id) => Areas.ContainsKey(id) ? new AreaBuilder(this, Areas[id]) : throw new Exception($"Area with id {id} does not exist");
  public OpsBuilder CreateArea(string id, Action<AreaBuilder> action) { action(CreateArea(id)); return this; }
  public AreaBuilder CreateArea(string id)
  {
    Area area;
    if (!AllAreas.TryGetValue(id, out area)) {
      var go = new GameObject($"Area {id}");
      go.SetActive(false);
      area = go.AddComponent<Area>();
      area.identifier = id;
      area.enabled = false;
      // Areas should be parented under a dedicated Ops container
      var opsContainer = GameObject.Find("Ops") ?? new GameObject("Ops");
      area.transform.SetParent(opsContainer.transform);
    }
    area.enabled = true;
    AllAreas[id] = area;
    Areas[id] = area;
    return new AreaBuilder(this, area);
  }
  public OpsBuilder Area(string id, Action<AreaBuilder> action) { action(GetOrCreateArea(id)); return this; }
  public AreaBuilder Area(string id) => GetOrCreateArea(id);
  public OpsBuilder GetOrCreateArea(string id, Action<AreaBuilder> action) { action(GetOrCreateArea(id)); return this; }
  public AreaBuilder GetOrCreateArea(string id) => Areas.ContainsKey(id) ? GetArea(id) : CreateArea(id);
  public OpsBuilder RemoveArea(string id) {
    if (Areas.ContainsKey(id)) {
      Areas[id].enabled = false;
      Areas.Remove(id);
    }
    return this;
  }

  public OpsBuilder GetLoad(string id, Action<LoadBuilder> action) { action(GetLoad(id)); return this; }
  public LoadBuilder GetLoad(string id) => Loads.ContainsKey(id) ? new LoadBuilder(this, Loads[id]) : throw new Exception($"Load with id {id} does not exist");
  public OpsBuilder CreateLoad(string id, Action<LoadBuilder> action) { action(CreateLoad(id)); return this; }
  public LoadBuilder CreateLoad(string id)
  {
    Load load;
    if (!AllLoads.TryGetValue(id, out load)) {
      load = ScriptableObject.CreateInstance<Load>();
      load.name = id;
    }
    AllLoads[id] = load;
    Loads[id] = load;
    CarPrototypeLibrary.instance.opsLoads = Loads.Values.ToArray();
    return new LoadBuilder(this, load);
  }
  public OpsBuilder Load(string id, Action<LoadBuilder> action) { action(GetOrCreateLoad(id)); return this; }
  public LoadBuilder Load(string id) => GetOrCreateLoad(id);
  public OpsBuilder GetOrCreateLoad(string id, Action<LoadBuilder> action) { action(GetOrCreateLoad(id)); return this; }
  public LoadBuilder GetOrCreateLoad(string id) => Loads.ContainsKey(id) ? GetLoad(id) : CreateLoad(id);
  public OpsBuilder RemoveLoad(string id) {
    // Noop for loads, just remove from dictionary, don't destroy ScriptableObject
    if (Loads.ContainsKey(id)) {
      Loads.Remove(id);
    }
    CarPrototypeLibrary.instance.opsLoads = Loads.Values.ToArray();
    return this;
  }
}