using StrangeCustoms.Tracks;
using Track;

namespace MapEditor.Extensions
{
  public static class PatchEditorExtensions
  {

    public static void AddOrUpdateNode(this PatchEditor patchEditor, TrackNode trackNode)
    {
      patchEditor.AddOrUpdateNode(trackNode.id, trackNode.transform.localPosition, trackNode.transform.localEulerAngles, trackNode.flipSwitchStand);
    }

    public static void AddOrUpdateSegment(this PatchEditor patchEditor, TrackSegment trackSegment)
    {
      patchEditor.AddOrUpdateSegment(trackSegment.id, trackSegment.a.id, trackSegment.b.id, trackSegment.priority, trackSegment.groupId, trackSegment.speedLimit, trackSegment.style, trackSegment.trackClass);
    }

  }
}
