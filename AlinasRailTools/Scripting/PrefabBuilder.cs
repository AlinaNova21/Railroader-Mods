using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AlinasRailTools.Scripting;

public class PrefabBuilder(Helper helper)
{
  public Helper Helper => helper;
  public Helper Done() => helper;

  public static Dictionary<string, GameObject> AllTemplates = [];
  public static Dictionary<string, GameObject> Templates = [];

  public static Dictionary<string, GameObject> AllInstances = [];

  public static void ReloadCaches()
  {
    // Load templates from World/Prefabs container
    var prefabsContainer = GameObject.Find("World")?.transform.Find("Prefabs");
    if (prefabsContainer != null)
    {
      var templateObjects = prefabsContainer.GetComponentsInChildren<Transform>(true)
        .Where(t => t != prefabsContainer && t.parent == prefabsContainer)
        .Select(t => t.gameObject)
        .ToDictionary(go => go.name, go => go);

      AllTemplates = templateObjects;
      Templates = AllTemplates; // Keep all templates, even inactive ones, they are never active in the cache
                                //.Where(t => t.Value.activeInHierarchy)
                                //.ToDictionary(k => k.Key, v => v.Value);
    }

    // Validate instances - remove destroyed GameObjects
    var validInstances = new Dictionary<string, GameObject>();
    foreach (var kvp in AllInstances)
    {
      if (kvp.Value != null)
      {
        validInstances[kvp.Key] = kvp.Value;
      }
    }
    AllInstances = validInstances;
  }

  /// <summary>
  /// Load vanilla loader templates from the game scene
  /// </summary>
  public static void LoadVanillaLoaders()
  {
    var vanillaLoaderPaths = new Dictionary<string, string>
    {
      ["coalConveyor"] = "World/Large Scenery/Whittier/Coal Conveyor",
      ["coalTower"] = "World/Large Scenery/Bryson/Bryson Coaling Tower",
      ["dieselFuelingStand"] = "World/Large Scenery/Whittier/East Whittier Diesel Fueling Stand",
      ["waterTower"] = "World/Large Scenery/Whittier Water Tower",
      ["waterColumn"] = "World/Large Scenery/Whittier/Water Column"
    };

    // Use a temporary helper to access instance methods
    var tempHelper = new Helper("VanillaLoaderInit");

    foreach (var kvp in vanillaLoaderPaths)
    {
      var templateName = kvp.Key;
      var scenePath = kvp.Value;

      // Skip if template already exists
      if (AllTemplates.ContainsKey(templateName))
      {
        continue;
      }

      // Find the source GameObject in the scene
      var sourceObject = FindGameObjectByPath(scenePath);
      if (sourceObject != null)
      {
        tempHelper.Prefabs.CreateTemplate(templateName).From(sourceObject);
      }
    }
  }

  /// <summary>
  /// Load vanilla turntable template from the game scene
  /// </summary>
  public static void LoadVanillaTurntables()
  {
    var vanillaTurntablePath = "World/Large Scenery/Bryson/Bryson Turntable/30m Turntable";

    // Use a temporary helper to access instance methods
    var tempHelper = new Helper("VanillaTurntableInit");

    // Skip if template already exists
    if (!AllTemplates.ContainsKey("turntable"))
    {
      // Find the source GameObject in the scene
      var sourceObject = FindGameObjectByPath(vanillaTurntablePath);
      if (sourceObject != null)
      {
        tempHelper.Prefabs.CreateTemplate("turntable").From(sourceObject);
      }
    }
  }

  /// <summary>
  /// Load vanilla roundhouse piece templates from the game scene
  /// Stores individual vanilla pieces that will be assembled in RoundhouseInstanceBuilder
  /// </summary>
  public static void LoadVanillaRoundhouses()
  {
    var tempHelper = new Helper("VanillaRoundhouseInit");

    var vanillaRoundhousePieces = new Dictionary<string, string>
    {
      ["roundhouseStall"] = "World/Large Scenery/Bryson/Bryson Turntable/Roundhouse/Stall",
      ["roundhouseSide"] = "World/Large Scenery/Bryson/Bryson Turntable/Roundhouse/Roundhouse Modular A Side",
      ["roundhouseBetween"] = "World/Large Scenery/Bryson/Bryson Turntable/Roundhouse/Roundhouse Modular A Between"
    };

    foreach (var kvp in vanillaRoundhousePieces)
    {
      var templateName = kvp.Key;
      var scenePath = kvp.Value;

      // Skip if template already exists
      if (AllTemplates.ContainsKey(templateName))
      {
        continue;
      }

      // Find the source GameObject in the scene
      var sourceObject = FindGameObjectByPath(scenePath);
      if (sourceObject != null)
      {
        tempHelper.Prefabs.CreateTemplate(templateName).From(sourceObject);
      }
    }
  }

  private static GameObject FindGameObjectByPath(string path)
  {
    var segments = path.Split('/');
    if (segments.Length == 0) return null;

    var current = GameObject.Find(segments[0]);
    if (current == null) return null;

    for (int i = 1; i < segments.Length; i++)
    {
      var child = current.transform.Find(segments[i]);
      if (child == null) return null;
      current = child.gameObject;
    }

    return current;
  }

  public PrefabBuilder GetTemplate(string id, Action<PrefabTemplate> action) { action(GetTemplate(id)); return this; }
  public PrefabTemplate GetTemplate(string id) => Templates.ContainsKey(id) ? new PrefabTemplate(this, Templates[id]) : throw new Exception($"Template with id {id} does not exist");

  public PrefabBuilder CreateTemplate(string id, Action<PrefabTemplate> action) { action(CreateTemplate(id)); return this; }
  public PrefabTemplate CreateTemplate(string id)
  {
    GameObject template;
    if (!AllTemplates.TryGetValue(id, out template))
    {
      template = new GameObject(id);
      template.SetActive(false);

      // Parent to World/Prefabs container
      var world = GameObject.Find("World");
      if (world == null)
      {
        world = new GameObject("World");
      }
      var prefabsContainer = world.transform.Find("Prefabs")?.gameObject;
      if (prefabsContainer == null)
      {
        prefabsContainer = new GameObject("Prefabs");
        prefabsContainer.transform.SetParent(world.transform);
      }
      template.transform.SetParent(prefabsContainer.transform);
    }

    AllTemplates[id] = template;
    Templates[id] = template;
    return new PrefabTemplate(this, template);
  }

  public PrefabBuilder Template(string id, Action<PrefabTemplate> action) { action(GetOrCreateTemplate(id)); return this; }
  public PrefabTemplate Template(string id) => GetOrCreateTemplate(id);
  public PrefabBuilder GetOrCreateTemplate(string id, Action<PrefabTemplate> action) { action(GetOrCreateTemplate(id)); return this; }
  public PrefabTemplate GetOrCreateTemplate(string id) => Templates.ContainsKey(id) ? GetTemplate(id) : CreateTemplate(id);

  public PrefabBuilder RemoveTemplate(string id)
  {
    if (Templates.ContainsKey(id))
    {
      Templates.Remove(id);
    }
    return this;
  }

  public PrefabBuilder GetInstance(string id, Action<PrefabInstance> action) { action(GetInstance(id)); return this; }
  public PrefabInstance GetInstance(string id) => AllInstances.ContainsKey(id) ? new PrefabInstance(this, AllInstances[id]) : throw new Exception($"Instance with id {id} does not exist");

  public PrefabBuilder CreateInstance(string id, Action<PrefabInstance> action) { action(CreateInstance(id)); return this; }
  public PrefabInstance CreateInstance(string id)
  {
    GameObject instance;
    if (!AllInstances.TryGetValue(id, out instance) || instance == null)
    {
      instance = new GameObject(id);
      instance.SetActive(false);
    }

    AllInstances[id] = instance;
    return new PrefabInstance(this, instance);
  }

  public PrefabBuilder Instance(string id, Action<PrefabInstance> action) { action(GetOrCreateInstance(id)); return this; }
  public PrefabInstance Instance(string id) => GetOrCreateInstance(id);
  public PrefabBuilder GetOrCreateInstance(string id, Action<PrefabInstance> action) { action(GetOrCreateInstance(id)); return this; }
  public PrefabInstance GetOrCreateInstance(string id) => AllInstances.ContainsKey(id) && AllInstances[id] != null ? GetInstance(id) : CreateInstance(id);

  public PrefabBuilder RemoveInstance(string id)
  {
    if (AllInstances.ContainsKey(id))
    {
      AllInstances.Remove(id);
    }
    return this;
  }
}