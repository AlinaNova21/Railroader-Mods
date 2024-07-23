using UnityEngine;

namespace MapEditor.StateTracker.Node
{
  public sealed class CreateTrackNode : IUndoable
  {

    private readonly string _id;
    private readonly TrackNodeGhost _ghost;

    public CreateTrackNode(string id, Vector3 position, Vector3 rotation, bool flipSwitchStand = false)
    {
      _id = id;
      _ghost = new TrackNodeGhost(id, position, rotation, flipSwitchStand);
    }

    public void Apply()
    {
      if (EditorContext.Settings.DebugLog)
      {
        Serilog.Log.ForContext<CreateTrackNode>().Information($"Apply({_id})");
      }

      _ghost.CreateNode();
    }

    public void Revert()
    {
      if (EditorContext.Settings.DebugLog)
      {
        Serilog.Log.ForContext<CreateTrackNode>().Information($"Revert({_id})");
      }

      _ghost.DestroyNode();
    }

    public override string ToString()
    {
      return "CreateTrackNode: " + _id;
    }

  }
}
