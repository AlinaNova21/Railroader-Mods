namespace MapEditor.StateTracker.Node {
  using JetBrains.Annotations;
  using Track;

  public sealed class DeleteTrackNode : IUndoable {

    private readonly string _id;
    private TrackNodeGhost _ghost;

    public DeleteTrackNode(TrackNode trackNode) {
      _id = trackNode.id!;
    }

    public void Apply() {
      _ghost = new TrackNodeGhost(_id);
      _ghost.DestroyNode();
    }

    public void Revert() {
      _ghost!.CreateNode();
    }

    public override string ToString() {
      return "DeleteTrackNode: " + _id;
    }

  }
}
