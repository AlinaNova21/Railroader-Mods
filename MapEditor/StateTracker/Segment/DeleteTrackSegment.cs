using Track;

namespace MapEditor.StateTracker.Segment
{
  public sealed class DeleteTrackSegment(TrackSegment trackSegment) : IUndoable
  {

    private readonly string _Id = trackSegment.id;
    private TrackSegmentGhost? _Ghost;

    public void Apply()
    {
      _Ghost = new TrackSegmentGhost(_Id);
      _Ghost.DestroySegment();
    }

    public void Revert()
    {
      _Ghost!.CreateSegment();
    }

    public override string ToString()
    {
      return "DeleteTrackNode: " + _Id;
    }

  }
}
