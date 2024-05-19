using Helpers;
using Track;
using UnityEngine;

namespace MapEditor.StateTracker
{
  public class TrackSegmentDeleted : TrackSegmentCreated
  {
    public TrackSegmentDeleted(string id, TrackNode a, TrackNode b, TrackSegment.Style style = TrackSegment.Style.Standard, string groupId = "") : base(id, a, b, style, groupId)
    {
    }

    public new void Apply()
    {
      base.Revert();
    }

    public new void Revert()
    {
      base.Apply();
    }
  }
}
