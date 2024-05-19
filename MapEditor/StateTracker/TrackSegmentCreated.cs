using Helpers;
using Track;
using UnityEngine;

namespace MapEditor.StateTracker
{
  public class TrackSegmentCreated : IUndoable
  {
    private TrackSegment _segment;
    private string _id;
    private TrackNode _a;
    private TrackNode _b;
    private TrackSegment.Style _style;
    private string _groupId;

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
      EditorContext.Instance.PatchEditor.AddOrUpdateSegment(_segment.id, _a.id, _b.id, 0, _groupId, 0, _style);
    }

    public void Revert()
    {
      UnityEngine.Object.Destroy(_segment.gameObject);
      Graph.Shared.RebuildCollections();
      EditorContext.Instance.PatchEditor.RemoveSegment(_id);
    }
  }
}
