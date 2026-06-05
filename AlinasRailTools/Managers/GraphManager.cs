using System.Collections.Generic;
using System.Linq;
using AlinasRailTools.Extensions;
using AlinasRailTools.Shared.Definitions;
using AlinasRailTools.Shared.Managers;
using AlinasRailTools.Shared.Resources;
using Newtonsoft.Json.Linq;
using Track;
using UnityEngine;

namespace AlinasRailTools.Managers;

/// <summary>
/// Manager for track graph objects (nodes, segments, spans)
/// Declares that areas need track spans to be loaded first
/// </summary>
[ResourceType("trackNodes", "trackSegments", "trackSpans")]
[ManagerBefore("areas")]
public class GraphManager : MonoBehaviour, IResourceManager
{
  public NodeManager Nodes { get; private set; }
  public SegmentManager Segments { get; private set; }
  public SpanManager Spans { get; private set; }

  // Shared caches
  private Dictionary<string, TrackNode> AllNodes = new();
  private Dictionary<string, TrackSegment> AllSegments = new();
  private Dictionary<string, TrackSpan> AllSpans = new();

  void Awake()
  {
    Nodes = new NodeManager(this);
    Segments = new SegmentManager(this);
    Spans = new SpanManager(this);
  }

  /// <summary>
  /// Reloads all track object caches from the scene
  /// </summary>
  public void ReloadCaches()
  {
    AllNodes.Clear();
    Object.FindObjectsByType<TrackNode>(FindObjectsInactive.Include, FindObjectsSortMode.None)
      .ToList()
      .ForEach(n => AllNodes[n.id] = n);

    AllSegments.Clear();
    Object.FindObjectsByType<TrackSegment>(FindObjectsInactive.Include, FindObjectsSortMode.None)
      .ToList()
      .ForEach(s => AllSegments[s.id] = s);

    AllSpans.Clear();
    Object.FindObjectsByType<TrackSpan>(FindObjectsInactive.Include, FindObjectsSortMode.None)
      .ToList()
      .ForEach(s => AllSpans[s.id] = s);
  }

  // Wrapper methods for convenience
  public TrackNode Apply(string id, SerializedTrackNode data) => Nodes.Apply(id, data);
  public TrackSegment Apply(string id, SerializedTrackSegment data) => Segments.Apply(id, data);
  public TrackSpan Apply(string id, SerializedTrackSpan data) => Spans.Apply(id, data);

  public SerializedTrackNode Serialize(string id, TrackNode _) => Nodes.Serialize(id);
  public SerializedTrackSegment Serialize(string id, TrackSegment _) => Segments.Serialize(id);
  public SerializedTrackSpan Serialize(string id, TrackSpan _) => Spans.Serialize(id);

  // Bulk operations
  public SerializedTrackData SerializeTracks()
  {
    return new SerializedTrackData
    {
      Nodes = Nodes.List().ToDictionary(n => n.id, n => Nodes.Serialize(n.id)),
      Segments = Segments.List().ToDictionary(s => s.id, s => Segments.Serialize(s.id)),
      Spans = Spans.List().ToDictionary(s => s.id, s => Spans.Serialize(s.id))
    };
  }

  public void ApplyTracks(SerializedTrackData data)
  {
    // Apply in correct order: nodes first, then segments/spans
    foreach (var kvp in data.Nodes ?? new Dictionary<string, SerializedTrackNode>())
    {
      Nodes.Apply(kvp.Key, kvp.Value);
    }
    foreach (var kvp in data.Segments ?? new Dictionary<string, SerializedTrackSegment>())
    {
      Segments.Apply(kvp.Key, kvp.Value);
    }
    foreach (var kvp in data.Spans ?? new Dictionary<string, SerializedTrackSpan>())
    {
      Spans.Apply(kvp.Key, kvp.Value);
    }
  }

  // IResourceManager implementation

  public string[] GetResourceTypes() => new[] { "trackNodes", "trackSegments", "trackSpans" };

  public void ApplyFromState(StateFile state, IResourceLocator locator)
  {
    // Apply in dependency order: nodes -> segments -> spans

    // 1. Apply nodes first (no dependencies)
    var nodes = state.GetResources("trackNodes");
    foreach (var kvp in nodes)
    {
      if (kvp.Value == null)
        Nodes.Remove(kvp.Key);
      else
        Nodes.Apply(kvp.Key, kvp.Value.ToObject<SerializedTrackNode>());
    }

    // 2. Apply segments (depend on nodes)
    var segments = state.GetResources("trackSegments");
    foreach (var kvp in segments)
    {
      if (kvp.Value == null)
        Segments.Remove(kvp.Key);
      else
        Segments.Apply(kvp.Key, kvp.Value.ToObject<SerializedTrackSegment>());
    }

    // 3. Apply spans (depend on segments)
    var spans = state.GetResources("trackSpans");
    foreach (var kvp in spans)
    {
      if (kvp.Value == null)
        Spans.Remove(kvp.Key);
      else
        Spans.Apply(kvp.Key, kvp.Value.ToObject<SerializedTrackSpan>());
    }
  }

  public void SnapshotToState(StateFile state)
  {
    // Snapshot nodes
    var nodeSnapshot = new Dictionary<string, JObject>();
    foreach (var kvp in AllNodes)
    {
      if (kvp.Value.enabled)
      {
        nodeSnapshot[kvp.Key] = JObject.FromObject(Nodes.Serialize(kvp.Key));
      }
    }
    state.SetResources("trackNodes", nodeSnapshot);

    // Snapshot segments
    var segmentSnapshot = new Dictionary<string, JObject>();
    foreach (var kvp in AllSegments)
    {
      if (kvp.Value.enabled)
      {
        segmentSnapshot[kvp.Key] = JObject.FromObject(Segments.Serialize(kvp.Key));
      }
    }
    state.SetResources("trackSegments", segmentSnapshot);

    // Snapshot spans
    var spanSnapshot = new Dictionary<string, JObject>();
    foreach (var kvp in AllSpans)
    {
      if (kvp.Value.enabled)
      {
        spanSnapshot[kvp.Key] = JObject.FromObject(Spans.Serialize(kvp.Key));
      }
    }
    state.SetResources("trackSpans", spanSnapshot);
  }

  // Nested NodeManager
  public class NodeManager : IEntityManager<TrackNode, SerializedTrackNode>
  {
    public GraphManager Shared { get; }

    public NodeManager(GraphManager shared) => Shared = shared;

    public TrackNode Apply(string id, SerializedTrackNode data)
    {
      if (!Shared.AllNodes.TryGetValue(id, out var node))
      {
        var go = new GameObject($"TrackNode {id}");
        go.SetActive(false);
        node = go.AddComponent<TrackNode>();
        node.id = id;
        node.enabled = false;
        node.transform.SetParent(Graph.Shared.transform);
        go.SetActive(true);
        Shared.AllNodes[id] = node;
      }

      node.transform.localPosition = data.Position.ToUnity();
      node.transform.localEulerAngles = data.Rotation.ToUnity();
      node.flipSwitchStand = data.FlipSwitchStand;
      node.enabled = true;

      return node;
    }

    public void Remove(string id)
    {
      if (Shared.AllNodes.TryGetValue(id, out var node))
      {
        node.enabled = false;
        Shared.AllNodes.Remove(id);
      }
    }

    public TrackNode Get(string id) => Shared.AllNodes[id];

    public SerializedTrackNode Serialize(string id)
    {
      var node = Get(id);
      return new SerializedTrackNode
      {
        Position = node.transform.localPosition.ToSerialized(),
        Rotation = node.transform.localEulerAngles.ToSerialized(),
        FlipSwitchStand = node.flipSwitchStand
      };
    }

    public List<TrackNode> List() => Shared.AllNodes.Values.Where(n => n.enabled).ToList();

    public bool Exists(string id) => Shared.AllNodes.ContainsKey(id);
  }

  // Nested SegmentManager
  public class SegmentManager : IEntityManager<TrackSegment, SerializedTrackSegment>
  {
    public GraphManager Shared { get; }

    public SegmentManager(GraphManager shared) => Shared = shared;

    public TrackSegment Apply(string id, SerializedTrackSegment data)
    {
      if (!Shared.AllSegments.TryGetValue(id, out var segment))
      {
        var go = new GameObject($"TrackSegment {id}");
        go.SetActive(false);
        segment = go.AddComponent<TrackSegment>();
        segment.id = id;
        segment.enabled = false;
        segment.transform.SetParent(Graph.Shared.transform);
        go.SetActive(true);
        Shared.AllSegments[id] = segment;
      }

      segment.a = Shared.AllNodes[data.StartId];
      segment.b = Shared.AllNodes[data.EndId];
      segment.style = (TrackSegment.Style)data.Style;
      segment.groupId = data.GroupId ?? "";
      segment.priority = data.Priority;
      segment.enabled = true;

      return segment;
    }

    public void Remove(string id)
    {
      if (Shared.AllSegments.TryGetValue(id, out var segment))
      {
        segment.enabled = false;
        Shared.AllSegments.Remove(id);
      }
    }

    public TrackSegment Get(string id) => Shared.AllSegments[id];

    public SerializedTrackSegment Serialize(string id)
    {
      var segment = Get(id);
      return new SerializedTrackSegment
      {
        StartId = segment.a.id,
        EndId = segment.b.id,
        Style = (TrackSegmentStyle)segment.style,
        GroupId = segment.groupId,
        Priority = segment.priority
      };
    }

    public List<TrackSegment> List() => Shared.AllSegments.Values.Where(s => s.enabled).ToList();

    public bool Exists(string id) => Shared.AllSegments.ContainsKey(id);
  }

  // Nested SpanManager
  public class SpanManager : IEntityManager<TrackSpan, SerializedTrackSpan>
  {
    public GraphManager Shared { get; }

    public SpanManager(GraphManager shared) => Shared = shared;

    public TrackSpan Apply(string id, SerializedTrackSpan data)
    {
      // TODO: Implement TrackSpan creation/update
      throw new System.NotImplementedException("TrackSpan Apply not yet implemented");
    }

    public void Remove(string id)
    {
      if (Shared.AllSpans.TryGetValue(id, out var span))
      {
        span.enabled = false;
        Shared.AllSpans.Remove(id);
      }
    }

    public TrackSpan Get(string id) => Shared.AllSpans[id];

    public SerializedTrackSpan Serialize(string id)
    {
      var span = Get(id);
      if (span == null) return null;

      return new SerializedTrackSpan
      {
        Upper = SerializeSpanPart(span.upper),
        Lower = SerializeSpanPart(span.lower)
      };
    }

    private TrackSpanPart SerializeSpanPart(object part)
    {
      if (part == null) return null;

      // Use dynamic to access properties since SpanPart type is not accessible
      dynamic spanPart = part;

      return new TrackSpanPart
      {
        SegmentId = spanPart.segment?.id ?? "",
        Distance = spanPart.distance,
        End = spanPart.end == 0 ? TrackSpanPartEnd.Start : TrackSpanPartEnd.End
      };
    }

    public List<TrackSpan> List() => Shared.AllSpans.Values.Where(s => s.enabled).ToList();

    public bool Exists(string id) => Shared.AllSpans.ContainsKey(id);
  }
}
