using System;
using System.Collections.Generic;
using System.Linq;
using AlinasMapMod.Caches;
using HarmonyLib;
using Helpers;
using Model;
using Model.Ops;
using Serilog;
using TMPro;
using UnityEngine;

namespace AlinasMapMod.Stations;

public partial class PaxStationAgent : MonoBehaviour
{
  readonly Serilog.ILogger logger = Log.ForContext<PaxStationAgent>();
  public string identifier;

  private string _prefab = "empty://";
  public string Prefab
  {
    get => _prefab;
    set {
      if (value == null) value = "empty://";
      _prefab = value;
      Rebuild();
    }
  }

  private string _passengerStop = "whittier";
  public string PassengerStop
  {
    get => _passengerStop;
    set {
      _passengerStop = value;
      Rebuild();
    }
  }

  public void Rebuild()
  {
    var go = transform.gameObject;
    
    if (Prefab == "") {
      logger.Error($"Station Agent prefab not set for {identifier}");
      return;
    }
    var prefab = Utils.GameObjectFromUri(Prefab);

    if (prefab == null) {
      Log.Error("Station Agent prefab not found: {prefab}", Prefab);
      throw new ArgumentException("Staion Agent prefab not found " + Prefab);
    }

    go.transform.DestroyAllChildren();

    var lgo = Instantiate(prefab, go.transform);
    lgo.name = "prefab";
    lgo.transform.localPosition = Vector3.zero;
    lgo.transform.localEulerAngles = Vector3.zero;
    go.name = identifier;

    go.SetActive(false);
    var stationAgent = lgo.GetComponentInChildren<StationAgent>(true);
    stationAgent.name = "Agent - " + identifier;
        if (!PassengerStopCache.Instance.TryGetValue(_passengerStop, out var passengerStop)) {
      throw new ArgumentException($"Passenger stop not found: {_passengerStop}");
    }
    var area = passengerStop.GetComponentInParent<Area>(true);
    var areaField = typeof(StationAgent).GetField("area", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    areaField.SetValue(stationAgent, area);
    logger.Information($"Set Station Agent area to {areaField.GetValue(stationAgent)} for {identifier}");

        var stopField =  typeof(StationAgent).GetField("passengerStop", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    stopField.SetValue(stationAgent, passengerStop);
        logger.Information($"Set Station Agent passenger stop to {stopField.GetValue(stationAgent)} for {identifier}");

        var secondaryAreas = AccessTools.Field(typeof(StationAgent), "secondaryAreas").GetValue(stationAgent) as List<Area>;
    secondaryAreas.Clear();

    var signs = lgo.GetComponentsInChildren<TextMeshPro>(true);
    foreach (var sign in signs) {
        if (sign.transform.parent.name.StartsWith("Sign-Station")) {
            sign.text = area.name;
            var sign2 = sign.transform.Find("Sign-Station");
            if (sign2 != null) {
                var scale = sign2.localScale;
                scale.y = 100;
                sign2.localScale = scale;
            }
        }
    }

    foreach (Renderer r in lgo.transform.GetComponentsInChildren<Renderer>()) {
      r.enabled = true; // enable all renderers
    }

    go.SetActive(true);
    lgo.SetActive(true);
  }

  public static PaxStationAgent FindById(string id)
  {
    StationAgentCache.Instance.TryGetValue(id, out var agent);
    return agent;
  }
}
