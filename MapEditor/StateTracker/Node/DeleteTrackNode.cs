using Track;

namespace MapEditor.StateTracker.Node
{
  public sealed class DeleteTrackNode(TrackNode trackNode) : IUndoable
  {

    private readonly string _Id = trackNode.id;
    private TrackNodeGhost? _Ghost;

    public void Apply()
    {
      _Ghost = new TrackNodeGhost(_Id);
      _Ghost.DestroyNode();
    }

    public void Revert()
    {
      _Ghost!.CreateNode();
    }

    public override string ToString()
    {
      return "DeleteTrackNode: " + _Id;
    }

  }
}
