using Track;

namespace MapEditor.StateTracker.Segment
{
  public sealed class CreateTrackSegment : IUndoable
  {

    private readonly string _id;
    private readonly TrackSegmentGhost _ghost;

    public CreateTrackSegment(string id, string a, string b, int priority = 0, int speedLimit = 0, string groupId = "", TrackSegment.Style style = TrackSegment.Style.Standard, TrackClass trackClass = TrackClass.Mainline)
    {
      _id = id;
      _ghost = new TrackSegmentGhost(id, a, b, priority, speedLimit, groupId, style, trackClass);
    }

    public void Apply()
    {
      _ghost.CreateSegment();
    }

    public void Revert()
    {
      _ghost.DestroySegment();
    }

    public override string ToString()
    {
      return "CreateTrackSegment: " + _id;
    }

  }
}
