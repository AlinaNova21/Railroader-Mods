namespace MapEditor.StateTracker.Segment {
  using JetBrains.Annotations;
  using MapEditor.Extensions;
  using Track;
  using UnityEngine;

  public sealed class TrackSegmentGhost {

    private readonly string _id;
    
    private string _a;
    private string _b;
    private int _priority;
    private int _speedLimit;
    private string _groupId;
    private TrackSegment.Style _style;
    private TrackClass _trackClass;

    public TrackSegmentGhost(string id) {
      _id = id;
    }

    public TrackSegmentGhost(string id, string a, string b, int priority = 0, int speedLimit = 0, string groupId = "", TrackSegment.Style style = TrackSegment.Style.Standard, TrackClass trackClass = TrackClass.Mainline)
      : this(id) {
      _a = a;
      _b = b;
      _priority = priority;
      _speedLimit = speedLimit;
      _groupId = groupId;
      _style = style;
      _trackClass = trackClass;
    }

    public void UpdateGhost(TrackSegment segment) {
      _a = segment.a.id;
      _b = segment.b.id;
      _priority = segment.priority;
      _speedLimit = segment.speedLimit;
      _groupId = segment.groupId;
      _style = segment.style;
      _trackClass = segment.trackClass;
    }

    public void UpdateSegment(TrackSegment segment) {
      segment.a = Graph.Shared.GetNode(_a);
      segment.b = Graph.Shared.GetNode(_b);
      segment.priority = _priority;
      segment.speedLimit = _speedLimit;
      segment.groupId = _groupId;
      segment.style = _style;
      segment.trackClass = _trackClass;
    }

    public void CreateSegment() {
      var segment = new GameObject($"Segment {_id}").AddComponent<TrackSegment>();
      segment.id = _id;
      segment.transform.SetParent(Graph.Shared.transform);
      UpdateSegment(segment);
      Graph.Shared.AddSegment(segment);
      EditorContext.Instance.PatchEditor.AddOrUpdateSegment(segment);
    }

    public void DestroySegment() {
      var segment = Graph.Shared.GetSegment(_id);
      UpdateGhost(segment);
      Object.Destroy(segment.gameObject);
      Graph.Shared.RebuildCollections();
      EditorContext.Instance.PatchEditor.RemoveSegment(_id);
    }

  }
}
