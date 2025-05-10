using System;
using System.Collections.Generic;
using System.Linq;
using AlinasMapMod.Definitions;
using HarmonyLib;
using Model;
using Model.Ops;
using Newtonsoft.Json.Linq;
using Serilog;
using TMPro;
using UnityEngine;

namespace AlinasMapMod;

public class StationAgentBuilder : StrangeCustoms.ISplineyBuilder
{
  readonly Serilog.ILogger logger = Log.ForContext<StationAgentBuilder>();

  public class PaxStationAgent: MonoBehaviour
  {
    readonly Serilog.ILogger logger = Log.ForContext<PaxStationAgent>();
    public string id;
    public SerializedStationAgent config;

    public void Rebuild()
    {
      var agent = config;
      var go = transform.gameObject;
      Vector3 pos = agent.Position;
      Vector3 rot = agent.Rotation;

      if (agent.Prefab == "")
      {
        logger.Error($"Station Agent prefab not set for {id}");
        return;
      }
      var prefab = Utils.GameObjectFromUri(agent.Prefab);

      if (prefab == null)
      {
        Log.Error("Station Agent prefab not found: {prefab}", agent.Prefab);
        throw new ArgumentException("Staion Agent prefab not found " + agent.Prefab);
      }

      var oldPrefab = transform.Find("prefab")?.gameObject;
      if (oldPrefab != null)
      {
        GameObject.Destroy(oldPrefab);
      }

      var lgo = GameObject.Instantiate(prefab, go.transform);
      lgo.name = "prefab";
      go.name = id;

      go.SetActive(false);
      go.transform.localPosition = pos;
      go.transform.localEulerAngles = rot;
      var stationAgent = go.GetComponentInChildren<StationAgent>(true);
      var passengerStop = GameObject.FindObjectsByType<PassengerStop>(FindObjectsSortMode.None)
        .SingleOrDefault(v => v.identifier == agent.PassengerStop);
      var area = GetComponentInParent<Area>(true);
      typeof(StationAgent).GetField("area", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
        ?.SetValue(stationAgent, area);
      typeof(StationAgent).GetField("passengerStop", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
        ?.SetValue(stationAgent, passengerStop);
      var secondaryAreas = AccessTools.Field(typeof(StationAgent), "secondaryAreas").GetValue(stationAgent) as List<Area>;
      secondaryAreas.Clear();

      var signs = lgo.GetComponentsInChildren<TextMeshPro>(true);
      foreach (var sign in signs)
      {
        if (sign.transform.parent.name.StartsWith("Sign-Station"))
        {
          sign.text = area.name;
        }
      }

      foreach (Renderer r in lgo.transform.GetComponentsInChildren<Renderer>())
      {
        r.enabled = true; // enable all renderers
      }

      go.SetActive(true);
      lgo.SetActive(true);
    }

    public static PaxStationAgent FindById(string id)
    {
      var go = GameObject.Find(id);
      if (go == null)
      {
        return null;
      }
      var cl = go.GetComponent<PaxStationAgent>();
      if (cl == null)
      {
        return null;
      }
      return cl;
    }
  }

  public GameObject BuildSpliney(string id, Transform parentTransform, JObject data)
  {
    var agent = data.ToObject<SerializedStationAgent>();

    logger.Information($"Configuring Station Agent {id} with prefab {agent.Prefab}");

    var parent = GameObject.Find("Large Scenery");
    var go = new GameObject(id);
    go.transform.parent = parent.transform;
    var cl = go.AddComponent<PaxStationAgent>();
    cl.id = id;
    cl.config = agent;
    cl.Rebuild();
    return go;
  }
}
