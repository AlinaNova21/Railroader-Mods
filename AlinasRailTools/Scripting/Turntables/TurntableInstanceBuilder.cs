using System;
using System.Collections.Generic;
using HarmonyLib;
using RollingStock.Controls;
using Serilog;
using Track;
using UnityEngine;

namespace AlinasRailTools.Scripting.Turntables;

public class TurntableInstanceBuilder : BaseBuilder<TurntableBuilder, TurntableInstanceBuilder>
{
  private readonly Serilog.ILogger logger = Log.ForContext<TurntableInstanceBuilder>();

  public TurntableInstance Turntable { get; }
  public Helper Helper => Parent.Helper;

  public TurntableInstanceBuilder(TurntableBuilder parent, TurntableInstance turntable)
    : base(parent, turntable.identifier)
  {
    Turntable = turntable;
  }

  public override TurntableBuilder Remove()
  {
    // Clean up turntable nodes
    if (Turntable.turntable != null)
    {
      var nodes = GetTurntableNodes(Turntable.turntable);
      if (nodes != null)
      {
        foreach (var node in nodes)
        {
          if (node != null)
          {
            Helper.Graph.RemoveTrackNode(node.id);
          }
        }
      }
    }

    // Clean up prefab instance
    if (Turntable.prefabInstance != null)
    {
      Helper.Prefabs.RemoveInstance(Turntable.prefabInstance.Id);
    }

    return Parent.RemoveTurntable(Id);
  }

  public override TurntableInstanceBuilder Invalidate()
  {
    // Invalidate doesn't auto-rebuild - Build() should be called explicitly when needed
    // This prevents redundant builds when multiple properties are changed
    return this;
  }

  // Properties - following TrackNodeBuilder pattern (Position/Rotation are local)
  public PrefabInstance PrefabInstance
  {
    get => Turntable.prefabInstance;
    set => Turntable.prefabInstance = value;
  }

  public int Radius
  {
    get => Turntable.radius;
    set => Turntable.radius = value;
  }

  public int Subdivisions
  {
    get => Turntable.subdivisions;
    set => Turntable.subdivisions = value;
  }

  public Vector3 Position
  {
    get => Turntable.transform.localPosition;
    set => Turntable.transform.localPosition = value;
  }

  public Quaternion Rotation
  {
    get => Turntable.transform.localRotation;
    set => Turntable.transform.localRotation = value;
  }

  public Vector3 Euler
  {
    get => Turntable.transform.localEulerAngles;
    set => Turntable.transform.localEulerAngles = value;
  }

  public Vector3 EulerAngles
  {
    get => Turntable.transform.localEulerAngles;
    set => Turntable.transform.localEulerAngles = value;
  }

  // Build method - creates Track.Turntable and nodes
  public TurntableInstanceBuilder Build()
  {
    logger.Information("Building turntable {Identifier} with radius={Radius} subdivisions={Subdivisions}",
      Id, Radius, Subdivisions);

    // Get or create Track.Turntable component
    var tt = Turntable.GetComponent<Track.Turntable>() ?? Turntable.gameObject.AddComponent<Track.Turntable>();
    tt.id = Id + ".turntable";
    tt.radius = Radius;
    tt.subdivisions = Subdivisions;

    // Create turntable nodes using helper.Graph
    var nodes = new List<TrackNode>();
    var nodesField = AccessTools.Field(typeof(Track.Turntable), "nodes");
    nodesField.SetValue(tt, nodes);

    var interval = 360f / tt.subdivisions;
    var turntablePos = Turntable.transform.position;
    var turntableRotY = Turntable.transform.eulerAngles.y;

    for (var i = 0; i < tt.subdivisions; i++)
    {
      var num = interval * i;
      var quaternion = Quaternion.Euler(0f, turntableRotY + num, 0f);

      // Create and position node around turntable perimeter
      var nodeId = $"N{Id}TurntableNode{i}";
      var nodePos = turntablePos + (quaternion * Vector3.forward * Radius);
      var nodeRot = new Vector3(0, turntableRotY + num, 0);

        var node = Helper.Graph.TrackNode(nodeId)
            .At(nodePos)
            .WithRotation(nodeRot)
            .SetActive(true)
            .Node;

      nodes.Add(node);
      node.turntable = tt;
    }

    // Set up prefab instance if it exists
    if (PrefabInstance != null && false) // Temporarily disable prefab instance setup until we have proper turntable prefabs
    {
      var gkvo = PrefabInstance.Instance.GetComponent<GlobalKeyValueObject>();
      if (gkvo != null)
      {
        gkvo.globalObjectId = tt.id;
      }

      var controller = PrefabInstance.Instance.GetComponent<TurntableController>();
      if (controller != null)
      {
        controller.turntable = tt;
      }

      // Enable all renderers (except pit collider)
      EnableRenderers(PrefabInstance.Instance);

      // IMPORTANT: Only activate after nodes are attached
      PrefabInstance.SetActive(true);
    }

    Turntable.turntable = tt;
    return this;
  }

  // Helper method to get turntable nodes using reflection
  private List<TrackNode> GetTurntableNodes(Track.Turntable turntable)
  {
    var nodesField = AccessTools.Field(typeof(Track.Turntable), "nodes");
    return nodesField.GetValue(turntable) as List<TrackNode>;
  }

  // Helper method to enable all renderers on a GameObject (except colliders)
  private void EnableRenderers(GameObject go)
  {
    foreach (var renderer in go.GetComponentsInChildren<Renderer>())
    {
      // Skip colliders - they should remain disabled
      var lowerName = renderer.name.ToLowerInvariant();
      if (lowerName.Contains("collider"))
      {
        continue;
      }
      renderer.enabled = true;
    }
  }

  // Fluent methods
  public TurntableInstanceBuilder WithPrefabInstance(PrefabInstance prefabInstance)
  {
    PrefabInstance = prefabInstance;
    return this;
  }

  public TurntableInstanceBuilder WithRadius(int radius)
  {
    Radius = radius;
    return this;
  }

  public TurntableInstanceBuilder WithSubdivisions(int subdivisions)
  {
    Subdivisions = subdivisions;
    return this;
  }

  public TurntableInstanceBuilder At(Vector3 position)
  {
    Position = position;
    return this;
  }

  public TurntableInstanceBuilder At(float x, float y, float z) => At(new Vector3(x, y, z));

  public TurntableInstanceBuilder WithRotation(Vector3 rotation)
  {
    Euler = rotation;
    return this;
  }

  public TurntableInstanceBuilder WithRotation(float x, float y, float z) => WithRotation(new Vector3(x, y, z));

  public TurntableInstanceBuilder WithRotation(Quaternion rotation)
  {
    Rotation = rotation;
    return this;
  }

  public TurntableInstanceBuilder WithRotation(float y) => WithRotation(Euler.x, y, Euler.z);

  public override TurntableInstanceBuilder SetActive(bool active = true)
  {
    Turntable.gameObject.SetActive(active);
    if (PrefabInstance != null)
    {
      PrefabInstance.SetActive(active);
    }
    return this;
  }
}
