using System.Security.Cryptography;
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
      if (EditorContext.Settings.DebugLog)
      {
       Serilog.Log.ForContext<DeleteTrackNode>().Information($"Apply({_Id})");
      }

      _Ghost!.CreateNode();
    }

    public override string ToString()
    {
      if (EditorContext.Settings.DebugLog)
      {
        Serilog.Log.ForContext<DeleteTrackNode>().Information($"Apply({_Id})");
      }

      return "DeleteTrackNode: " + _Id;
    }

  }
}
