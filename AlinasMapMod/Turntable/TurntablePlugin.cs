using AlinasMapMod.Definitions;
using GalaSoft.MvvmLight.Messaging;
using Railloader;
using Serilog;
using StrangeCustoms;
using StrangeCustoms.Tracks;
using Track;
using UnityEngine;

namespace AlinasMapMod.Turntable;

public partial class TurntablePlugin : SingletonPluginBase<TurntablePlugin>
{
  Serilog.ILogger logger = Log.ForContext<TurntablePlugin>();

  public TurntablePlugin(IModdingContext _moddingContext, IModDefinition self)
  {
  }

  public override void OnEnable()
  {
    Messenger.Default.Register<GraphWillChangeEvent>(this, HandleGraphWillChange);
    Messenger.Default.Register<GraphJsonWillDeserializeEvent>(this, HandleGraphJsonWillDeserialize);
  }

  private void HandleGraphJsonWillDeserialize(GraphJsonWillDeserializeEvent @event)
  {
    return;
  }

  private void HandleGraphWillChange(GraphWillChangeEvent @event)
  {
    // return;
    var state = @event.State;
    var splineys = state.Splineys;
    foreach (var id in splineys.Keys) {
      var data = splineys[id];
      if (data == null) continue; // Skip nulls
      var handler = data.Value<string>("handler");
      if (handler != "AlinasMapMod.Turntable.TurntableBuilder") continue;
      var turntable = data.ToObject<SerializedTurntable>();
      logger.Information($"Found turntable {id} ({turntable.RoundhouseStalls} stalls), generating tracks");
      var pos = new Vector3(turntable.Position.x, turntable.Position.y, turntable.Position.z);
      var rot = new Vector3(turntable.Rotation.x, turntable.Rotation.y, turntable.Rotation.z);
      var interval = 360f / turntable.Subdivisions;
      var radius = turntable.Radius;

      for (var i = 0; i < turntable.Subdivisions; i++) {
        var num = interval * i;
        Quaternion quaternion = Quaternion.Euler(0f, rot.y + num, 0f);
        var nodeId = $"N{id}TurntableNode{i}";
        SerializedNode node;
        if (!state.Tracks.Nodes.TryGetValue(nodeId, out node)) {
          node = state.Tracks.Nodes[nodeId] = new SerializedNode();
        }
        node.Position = pos + (quaternion * Vector3.forward * radius);
        node.Rotation = new Vector3(0, rot.y + num, 0);
        @event.MarkChanged(["tracks", "nodes", nodeId]);
      }

      for (var i = 1; i <= turntable.RoundhouseStalls; i++) {
        var dist = turntable.RoundhouseTrackLength + radius;
        var num = interval * i;
        Quaternion quaternion = Quaternion.Euler(0f, rot.y + num, 0f);

        var nodeId = $"N{id}RoundhouseNode{i}";
        SerializedNode node;
        if (!state.Tracks.Nodes.TryGetValue(nodeId, out node)) {
          node = state.Tracks.Nodes[nodeId] = new SerializedNode();
        }
        node.Position = pos + (quaternion * Vector3.forward * dist);
        node.Rotation = new Vector3(0, rot.y + num, 0);
        @event.MarkChanged(["tracks", "nodes", nodeId]);

        var segmentId = $"S{id}RoundhouseSegment{i}";
        SerializedSegment segment;
        if (!state.Tracks.Segments.TryGetValue(segmentId, out segment)) {
          segment = state.Tracks.Segments[segmentId] = new SerializedSegment();
        }
        segment.StartId = $"N{id}TurntableNode{i}";
        segment.EndId = nodeId;
        segment.Style = TrackSegment.Style.Yard;
        @event.MarkChanged(["tracks", "segments", segmentId]);
      }
    }
  }
}
