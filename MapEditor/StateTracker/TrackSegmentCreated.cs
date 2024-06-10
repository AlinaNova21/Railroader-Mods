using System;
using Track;
using UnityEngine;

namespace MapEditor.StateTracker
{
  [Obsolete("Replaced by as CreateTrackSegment")]
  public class TrackSegmentCreated : IUndoable
  {

    private TrackSegment _segment;
    private readonly string _id;
    private readonly TrackNode _a;
    private readonly TrackNode _b;
    private readonly TrackSegment.Style _style;
    private readonly string _groupId;

    public TrackSegmentCreated(string id, TrackNode a, TrackNode b, TrackSegment.Style style = TrackSegment.Style.Standard, string groupId = "")
    {
      _id = id;
      _a = a;
      _b = b;
      _style = style;
      _groupId = groupId;
    }

    public void Apply()
    {
      var newSegment = new GameObject($"Segment {_id}").AddComponent<TrackSegment>();
      newSegment.id = _id;
      newSegment.transform.SetParent(Graph.Shared.transform);
      newSegment.a = _a;
      newSegment.b = _b;
      newSegment.style = _style;
      newSegment.groupId = _groupId;
      Graph.Shared.AddSegment(newSegment);
      _segment = newSegment;
      EditorContext.PatchEditor.AddOrUpdateSegment(_segment.id, _a.id, _b.id, 0, _groupId, 0, _style);
    }

    public void Revert()
    {
      UnityEngine.Object.Destroy(_segment.gameObject);
      Graph.Shared.RebuildCollections();
      EditorContext.PatchEditor.RemoveSegment(_id);
    }

  }
}
