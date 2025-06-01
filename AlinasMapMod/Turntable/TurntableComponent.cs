using System.Collections.Generic;
using HarmonyLib;
using RollingStock.Controls;
using Serilog;
using Track;
using UnityEngine;

namespace AlinasMapMod.Turntable;
public class TurntableComponent : MonoBehaviour
{
  public string Identifier { get; set; }
  public int Radius { get; set; }
  public int Subdivisions { get; set; }

  public void OnEnable()
  {
    this.Build();
  }

  public void Build()
  {
    Log.Information("Building turntable {Identifier} {Radius} {Subdivisions}", Identifier, Radius, Subdivisions);
    var tt = this.GetComponent<Track.Turntable>() ?? this.gameObject.AddComponent<Track.Turntable>();
    tt.id = Identifier + ".turntable";
    tt.radius = Radius;
    tt.subdivisions = Subdivisions;

    var nodes = new List<TrackNode>();
    var nodesField = AccessTools.Field(typeof(Track.Turntable), "nodes");
    nodesField.SetValue(tt, nodes);
    for (var i = 0; i < tt.subdivisions; i++) {
      var nodeId = $"N{Identifier}TurntableNode{i}";
      var node = Graph.Shared.GetNode(nodeId);
      nodes.Add(node);
      node.turntable = tt;
    }

    if (!this.transform.Find("30m Turntable(Clone)")) {
      Log.Information("Instantiating turntable prefab for {Identifier}", Identifier);
      var turntablePrefab = GameObject.Find("30m Turntable");
      var ttInstance = Instantiate(turntablePrefab, this.transform);
      ttInstance.GetComponent<GlobalKeyValueObject>().globalObjectId = tt.id;
      ttInstance.GetComponent<TurntableController>().turntable = tt;
    }
  }
}
