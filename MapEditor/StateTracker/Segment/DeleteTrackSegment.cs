namespace MapEditor.StateTracker.Segment {
  using Track;

  public sealed class DeleteTrackSegment : IUndoable {

    private readonly string _id;
    private TrackSegmentGhost? _ghost;

    public DeleteTrackSegment(TrackSegment trackSegment) {
      _id = trackSegment.id!;
    }

    public void Apply() {
      _ghost = new TrackSegmentGhost(_id);
      _ghost.DestroySegment();
    }

    public void Revert() {
      _ghost!.CreateSegment();
    }

    public override string ToString() {
      return "DeleteTrackNode: " + _id;
    }

  }
}
