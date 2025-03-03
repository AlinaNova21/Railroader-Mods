using System.Linq;
using AlinasMapMod.Definitions;
using Microsoft.SqlServer.Server;
using Newtonsoft.Json.Linq;
using Serilog;
using Track;
using UnityEngine;

namespace AlinasMapMod.Turntable;

public class TurntableBuilder : StrangeCustoms.ISplineyBuilder
{
  Serilog.ILogger logger = Log.ForContext<TurntableBuilder>();
  public GameObject BuildSpliney(string id, Transform parentTransform, JObject data)
  {
    logger.Information($"Configuring turntable {id}");
    var turntable = data.ToObject<SerializedTurntable>();
    var pos = new Vector3(turntable.Position.x, turntable.Position.y, turntable.Position.z);
    var rot = new Vector3(turntable.Rotation.x, turntable.Rotation.y, turntable.Rotation.z);

    var go = GameObject.FindObjectsOfType<TurntableComponent>(true).FirstOrDefault(tt => tt.Identifier == id)?.gameObject ?? new GameObject(id);

    if (go.transform.childCount == 0) {
      go.SetActive(false);
    } else {
      logger.Information("Turntable {id} already exists, updating", id);
    }
    go.transform.parent = Graph.Shared.transform;
    go.transform.localPosition = pos;
    go.transform.localEulerAngles = rot;

    var rhc = go.GetComponent<RoundhouseComponent>() ?? go.AddComponent<RoundhouseComponent>();
    rhc.Subdivisions = turntable.Subdivisions;
    rhc.Stalls = turntable.RoundhouseStalls;
    rhc.StartPrefab = LoadPrefab("roundhouseStart", turntable.StartPrefab);
    rhc.EndPrefab = LoadPrefab("roundhouseEnd", turntable.EndPrefab);
    rhc.StallPrefab = LoadPrefab("roundhouseStall", turntable.StallPrefab);
    rhc.Build();

    var ttc = go.GetComponent<TurntableComponent>() ?? go.AddComponent<TurntableComponent>();
    ttc.Identifier = id;
    ttc.Radius = turntable.Radius;
    ttc.Subdivisions = turntable.Subdivisions;
    ttc.Build();

    go.SetActive(true);
    logger.Information("Turntable {id} configured", id);
    return new GameObject();
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

public class TurntableBuilderComponent : MonoBehaviour
{
  public void OnEnable()
  {

  }
}

