using System;
using Track;

namespace MapEditor.StateTracker
{
  [Obsolete("Replaced by as DeleteTrackSegment")]
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
