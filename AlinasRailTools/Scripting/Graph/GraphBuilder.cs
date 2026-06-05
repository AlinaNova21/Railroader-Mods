using System;
using AlinasRailTools.Scripting;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Track;
using UnityEngine;
using System.Data;

namespace AlinasRailTools.Scripting.Graph;

public class GraphBuilder(Helper helper)
{
  public Helper Helper => helper;
  public Helper Done() => helper;
  
  public static Dictionary<string, TrackNode> AllTrackNodes = [];
  public static Dictionary<string, TrackNode> TrackNodes = [];

  public static Dictionary<string, TrackSegment> AllTrackSegments = [];

  public static Dictionary<string, TrackSegment> TrackSegments = [];

  public static Dictionary<string, TrackSpan> AllTrackSpans = [];

  public static Dictionary<string, TrackSpan> TrackSpans = [];

  public static void ReloadCaches()
  {
    AllTrackNodes.Clear();
    UnityEngine.Object
      .FindObjectsByType<TrackNode>(FindObjectsInactive.Include, FindObjectsSortMode.None)
      .ToList()
      .ForEach(n => AllTrackNodes[n.id] = n);

    TrackNodes = AllTrackNodes
      .Where(n => n.Value.enabled)
      .ToDictionary(k => k.Key, v => v.Value);
    
    AllTrackSegments.Clear();
    UnityEngine.Object
      .FindObjectsByType<TrackSegment>(FindObjectsInactive.Include, FindObjectsSortMode.None)
      .ToList()
      .ForEach(s => AllTrackSegments[s.id] = s);

    TrackSegments = AllTrackSegments
      .Where(s => s.Value.enabled)
      .ToDictionary(k => k.Key, v => v.Value);

    AllTrackSpans.Clear();
    UnityEngine.Object
      .FindObjectsByType<TrackSpan>(FindObjectsInactive.Include, FindObjectsSortMode.None)
      .ToList()
      .ForEach(s => AllTrackSpans[s.id] = s);

    TrackSpans = AllTrackSpans
      .Where(s => s.Value.enabled)
      .ToDictionary(k => k.Key, v => v.Value);
  }

  public TrackSegment.Style SegmentStyle { get; set; } = TrackSegment.Style.Standard;
  public string GroupId { get; set; } = "";

  public GraphBuilder Reset()
  {
    SegmentStyle = TrackSegment.Style.Standard;
    GroupId = "";
    return this;
  }

  public GraphBuilder WithStyle(TrackSegment.Style style) { SegmentStyle = style; return this; }
  public GraphBuilder WithGroupId(string group) { GroupId = group; return this; }

  public GraphBuilder GetTrackNode(string id, Action<TrackNodeBuilder> action) { action(GetTrackNode(id)); return this; }
  public TrackNodeBuilder GetTrackNode(string id) => TrackNodes.ContainsKey(id) ? new TrackNodeBuilder(this, TrackNodes[id]) : throw new Exception($"TrackNode with id {id} does not exist");
  public GraphBuilder CreateTrackNode(string id, Action<TrackNodeBuilder> action) { action(CreateTrackNode(id)); return this; }
  public TrackNodeBuilder CreateTrackNode(string id)
  {
    TrackNode node;
    if (!AllTrackNodes.TryGetValue(id, out node)) {
      var go = new GameObject($"TrackNode {id}");
      go.SetActive(false);
      node = go.AddComponent<TrackNode>();
      node.id = id;
      node.enabled = false;
      node.transform.SetParent(Track.Graph.Shared.transform);
      node.gameObject.SetActive(true);
    }
    node.enabled = true;
    AllTrackNodes[id] = node; // Ensure it's in the AllNodes dictionary
    TrackNodes[id] = node; // Ensure it's in the Nodes dictionary
    return new TrackNodeBuilder(this, node);
  }
  public GraphBuilder TrackNode(string id, Action<TrackNodeBuilder> action) { action(GetOrCreateTrackNode(id)); return this; }
  public TrackNodeBuilder TrackNode(string id) => GetOrCreateTrackNode(id);
  public GraphBuilder GetOrCreateTrackNode(string id, Action<TrackNodeBuilder> action) { action(GetOrCreateTrackNode(id)); return this; }
  public TrackNodeBuilder GetOrCreateTrackNode(string id) => TrackNodes.ContainsKey(id) ? GetTrackNode(id) : CreateTrackNode(id);
  public GraphBuilder RemoveTrackNode(string id) {
    if (TrackNodes.ContainsKey(id)) {
      TrackNodes[id].enabled = false;
      TrackNodes.Remove(id);
    }
    return this;
  }

  public GraphBuilder GetTrackSegment(string id, Action<TrackSegmentBuilder> action) { action(GetTrackSegment(id)); return this; }
  public TrackSegmentBuilder GetTrackSegment(string id) => TrackSegments.ContainsKey(id) ? new TrackSegmentBuilder(this, TrackSegments[id]) : throw new Exception($"TrackSegment with id {id} does not exist");
  public GraphBuilder CreateTrackSegment(string id, TrackNode startNode, TrackNode endNode, Action<TrackSegmentBuilder> action) { action(CreateTrackSegment(id, startNode, endNode)); return this; }
  public TrackSegmentBuilder CreateTrackSegment(string id, TrackNode startNode, TrackNode endNode)
  {
    TrackSegment segment;
    if (!AllTrackSegments.TryGetValue(id, out segment)) {
      var go = new GameObject($"TrackSegment {id}");
      go.SetActive(false);
      segment = go.AddComponent<TrackSegment>();
      segment.id = id;
      segment.enabled = false;
      segment.gameObject.SetActive(true);
    }
    segment.transform.SetParent(Track.Graph.Shared.transform);
    segment.a = startNode;
    segment.b = endNode;
    segment.style = SegmentStyle;
    segment.groupId = GroupId;
    segment.enabled = true;
    AllTrackSegments[id] = segment; // Ensure it's in the AllSegments dictionary
    TrackSegments[id] = segment; // Ensure it's in the Segments dictionary
    return new TrackSegmentBuilder(this, segment);
  }
  public GraphBuilder GetOrCreateTrackSegment(string id, TrackNode startNode, TrackNode endNode, Action<TrackSegmentBuilder> action) { action(GetOrCreateTrackSegment(id, startNode, endNode)); return this; }
  public TrackSegmentBuilder GetOrCreateTrackSegment(string id, TrackNode startNode, TrackNode endNode) => TrackSegments.ContainsKey(id) ? GetTrackSegment(id) : CreateTrackSegment(id, startNode, endNode);
  public GraphBuilder RemoveTrackSegment(string id) {
    if (TrackSegments.ContainsKey(id)) {
      TrackSegments[id].enabled = false;
      TrackSegments.Remove(id);
    }
    return this;
  }

  public GraphBuilder GetTrackSpan(string id, Action<TrackSpanBuilder> action) { action(GetTrackSpan(id)); return this; }
  public TrackSpanBuilder GetTrackSpan(string id) => TrackSpans.ContainsKey(id) ? new TrackSpanBuilder(this, TrackSpans[id]) : throw new Exception($"TrackSpan with id {id} does not exist");
  public GraphBuilder CreateTrackSpan(string id, Action<TrackSpanBuilder> action) { action(CreateTrackSpan(id)); return this; }
  public TrackSpanBuilder CreateTrackSpan(string id)
  {
    TrackSpan span;
    if (!AllTrackSpans.TryGetValue(id, out span)) {
      var go = new GameObject($"TrackSpan {id}");
      go.SetActive(false);
      span = go.AddComponent<TrackSpan>();
      span.id = id;
      span.enabled = false;
      span.transform.SetParent(Track.Graph.Shared.transform);
      span.gameObject.SetActive(true);
    }
    span.enabled = true;
    AllTrackSpans[id] = span; // Ensure it's in the AllSpans dictionary
    TrackSpans[id] = span; // Ensure it's in the Spans dictionary
    return new TrackSpanBuilder(this, span);
  }
  public GraphBuilder TrackSpan(string id, Action<TrackSpanBuilder> action) { action(GetOrCreateTrackSpan(id)); return this; }
  public TrackSpanBuilder TrackSpan(string id) => GetOrCreateTrackSpan(id);
  public GraphBuilder GetOrCreateTrackSpan(string id, Action<TrackSpanBuilder> action) { action(GetOrCreateTrackSpan(id)); return this; }
  public TrackSpanBuilder GetOrCreateTrackSpan(string id) => TrackSpans.ContainsKey(id) ? GetTrackSpan(id) : CreateTrackSpan(id);
  public GraphBuilder RemoveTrackSpan(string id) {
    if (TrackSpans.ContainsKey(id)) {
      TrackSpans[id].enabled = false;
      TrackSpans.Remove(id);
    };
    return this;
  }
}
