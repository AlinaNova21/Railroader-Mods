using MapEditor.StateTracker.Segment;
using Track;

namespace MapEditor.Managers
{
  public static class SegmentManager
  {

    public static void Move(Direction direction)
    {
      // SelectedNode is already moved by NodeManager
      if (EditorContext.SelectedSegment!.a.id != EditorContext.SelectedNode?.id) {
        NodeManager.Move(direction, EditorContext.SelectedSegment.a);
      }

      if (EditorContext.SelectedSegment.b.id != EditorContext.SelectedNode?.id) {
        NodeManager.Move(direction, EditorContext.SelectedSegment.b);
      }
    }

    public static void UpdatePriority(int priority, TrackSegment? segment = null)
    {
      segment ??= EditorContext.SelectedSegment;
      EditorContext.ChangeManager.AddChange(new ChangeTrackSegment(segment!).Priority(priority));
    }

    public static void UpdateSpeedLimit(int speedLimit, TrackSegment? segment = null)
    {
      segment ??= EditorContext.SelectedSegment;
      EditorContext.ChangeManager.AddChange(new ChangeTrackSegment(segment!).SpeedLimit(speedLimit));
    }

    public static void UpdateGroup(string groupId, TrackSegment? segment = null)
    {
      segment ??= EditorContext.SelectedSegment;
      EditorContext.ChangeManager.AddChange(new ChangeTrackSegment(segment!).GroupId(groupId));
    }

    public static void UpdateStyle(TrackSegment.Style style, TrackSegment? segment = null)
    {
      segment ??= EditorContext.SelectedSegment;
      EditorContext.ChangeManager.AddChange(new ChangeTrackSegment(segment!).Style(style));
      Rebuild();
    }


    public static void UpdateTrackClass(TrackClass trackClass, TrackSegment? segment = null)
    {
      segment ??= EditorContext.SelectedSegment;
      EditorContext.ChangeManager.AddChange(new ChangeTrackSegment(segment!).TrackClass(trackClass));
    }

    public static void FlipSegment(TrackSegment? segment = null)
    {
      segment ??= EditorContext.SelectedSegment;
      EditorContext.ChangeManager.AddChange(new ChangeTrackSegment(segment!).Flip());
    }

    public static void RemoveSegment(TrackSegment? segment = null)
    {
      segment ??= EditorContext.SelectedSegment;
      EditorContext.ChangeManager.AddChange(new DeleteTrackSegment(segment!));
      Rebuild();
    }

    private static void Rebuild()
    {
      // not sure why this is not working, but calling same method from 'Rebuild Track' button works ...
      Graph.Shared.RebuildCollections();
      TrackObjectManager.Instance.Rebuild();
    }
  }
}
