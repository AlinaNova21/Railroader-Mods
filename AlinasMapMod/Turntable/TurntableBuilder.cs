using System;
using System.Linq;
using AlinasMapMod.Definitions;
using AlinasMapMod.MapEditor;
using AlinasMapMod.Validation;
using Newtonsoft.Json.Linq;
using StrangeCustoms.Tracks;
using Track;
using UnityEngine;

namespace AlinasMapMod.Turntable;

public class TurntableBuilder : SplineyBuilderBase, IObjectFactory
{
  public string Name => "Turntable";
  public bool Enabled => false;
  public Type ObjectType => typeof(TurntableComponent);

  protected override GameObject BuildSplineyInternal(string id, Transform parentTransform, JObject data)
  {
    return SafeBuildSplineyWithCleanup(id, parentTransform, data, (transaction) =>
    {
      var turntable = DeserializeAndValidate<SerializedTurntable>(data);
      Logger.Information("Configuring turntable {Id}", id);
      
      var pos = turntable.Position;
      var rot = turntable.Rotation;

      // Check if turntable already exists using cache-aware lookup
      var existingTurntable = FindExistingComponent<TurntableComponent>(id);
      GameObject go;

      if (existingTurntable != null)
      {
        go = existingTurntable.gameObject;
        Logger.Information("Turntable {Id} already exists, updating", id);
      }
      else
      {
        go = new GameObject(id);
        // Track new GameObject for cleanup on failure
        transaction.SetRootGameObject(go);
      }

      var et = GetOrAddComponent<EditableTurntable>(go);

      if (go.transform.childCount == 0) {
        go.SetActive(true);
      }
      go.transform.parent = Graph.Shared.transform;
      go.transform.localPosition = pos;
      go.transform.localEulerAngles = rot;

      ConfigureWithActivation(go, () =>
      {
        var rhc = GetOrAddComponent<RoundhouseComponent>(go);
        rhc.Subdivisions = turntable.Subdivisions;
        rhc.Stalls = turntable.RoundhouseStalls;
        rhc.StartPrefab = LoadPrefab("roundhouseStart", turntable.StartPrefab);
        rhc.EndPrefab = LoadPrefab("roundhouseEnd", turntable.EndPrefab);
        rhc.StallPrefab = LoadPrefab("roundhouseStall", turntable.StallPrefab);
        rhc.Build();

        var ttc = GetOrAddComponent<TurntableComponent>(go);
        ttc.Identifier = id;
        ttc.Radius = turntable.Radius;
        ttc.Subdivisions = turntable.Subdivisions;
        ttc.Build();
      });
      Logger.Information("Turntable {Id} configured", id);
      
      
      return go;
    });
  }

  public IEditableObject CreateObject(PatchEditor editor, string id)
  {
    // Return a default turntable configuration
    var turntable = new SerializedTurntable
    {
      Position = Vector3.zero,
      Rotation = Vector3.zero,
      Radius = 10,
      Subdivisions = 8
    };
    
    var go = BuildSplineyInternal(id, null, JObject.FromObject(turntable));
    return go.GetComponent<EditableTurntable>();
  }

  private GameObject LoadPrefab(string type, string path)
  {
    if (path == "")
      return new GameObject();

    if (path == "vanilla")
      return VanillaPrefabs.GetPrefab(type);

    return Utils.GameObjectFromUri(path);
  }
}

public class TurntableBuilderComponent : MonoBehaviour;

