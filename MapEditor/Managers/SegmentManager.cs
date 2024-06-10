using MapEditor.StateTracker.Segment;
using Track;

namespace MapEditor.Managers
{
  public static class SegmentManager
  {

    public static void UpdatePriority(int priority)
    {
      EditorContext.ChangeManager.AddChange(new ChangeTrackSegment(EditorContext.SelectedSegment!).Priority(priority));
    }

    public static void UpdateSpeedLimit(int speedLimit)
    {
      EditorContext.ChangeManager.AddChange(new ChangeTrackSegment(EditorContext.SelectedSegment!).SpeedLimit(speedLimit));
    }

    public static void UpdateGroup(string groupId)
    {
      EditorContext.ChangeManager.AddChange(new ChangeTrackSegment(EditorContext.SelectedSegment!).GroupId(groupId));
    }

    public static void UpdateStyle(TrackSegment.Style style)
    {
      EditorContext.ChangeManager.AddChange(new ChangeTrackSegment(EditorContext.SelectedSegment!).Style(style));
      Rebuild();
    }


    public static void UpdateTrackClass(TrackClass trackClass)
    {
      EditorContext.ChangeManager.AddChange(new ChangeTrackSegment(EditorContext.SelectedSegment!).TrackClass(trackClass));
    }

    private static void Rebuild()
    {
      // not sure why this is not working, but calling same method from 'Rebuild Track' button works ...
      Graph.Shared.RebuildCollections();
      TrackObjectManager.Instance.Rebuild();
    }

  }
}
