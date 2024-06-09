using MapEditor.StateTracker.Segment;
using Track;

namespace MapEditor.Managers
{
  public static class SegmentManager
  {

    public static void UpdatePriority(int priority)
    {
      EditorContext.Instance.ChangeManager.AddChange(new ChangeTrackSegment(EditorContext.Instance.SelectedSegment!).Priority(priority));
    }

    public static void UpdateSpeedLimit(int speedLimit)
    {
      EditorContext.Instance.ChangeManager.AddChange(new ChangeTrackSegment(EditorContext.Instance.SelectedSegment!).SpeedLimit(speedLimit));
    }

    public static void UpdateGroup(string groupId)
    {
      EditorContext.Instance.ChangeManager.AddChange(new ChangeTrackSegment(EditorContext.Instance.SelectedSegment!).GroupId(groupId));
    }

    public static void UpdateStyle(TrackSegment.Style style)
    {
      EditorContext.Instance.ChangeManager.AddChange(new ChangeTrackSegment(EditorContext.Instance.SelectedSegment!).Style(style));
      Rebuild();
    }


    public static void UpdateTrackClass(TrackClass trackClass)
    {
      EditorContext.Instance.ChangeManager.AddChange(new ChangeTrackSegment(EditorContext.Instance.SelectedSegment!).TrackClass(trackClass));
    }

    private static void Rebuild()
    {
      // not sure why this is not working, but calling same method from 'Rebuild Track' button works ...
      Graph.Shared.RebuildCollections();
      TrackObjectManager.Instance.Rebuild();
    }

  }
}
