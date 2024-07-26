using MapEditor.StateTracker.Node;
using Track;

namespace MapEditor.StateTracker.Segment
{
  public sealed class DeleteTrackSegment(TrackSegment trackSegment) : IUndoable
  {

    private readonly string _Id = trackSegment.id;
    private TrackSegmentGhost? _Ghost;

    public void Apply()
    {
      if (EditorContext.Settings.DebugLog)
      {
        Serilog.Log.ForContext<DeleteTrackSegment>().Information($"Apply({_Id})");
      }
      _Ghost = new TrackSegmentGhost(_Id);
      _Ghost.DestroySegment();
    }

    public void Revert()
    {
      if (EditorContext.Settings.DebugLog)
      {
        Serilog.Log.ForContext<DeleteTrackSegment>().Information($"Revert({_Id})");
      }
      _Ghost!.CreateSegment();
    }

    public override string ToString()
    {
      return "DeleteTrackNode: " + _Id;
    }

  }
}
