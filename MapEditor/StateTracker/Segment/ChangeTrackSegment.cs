using System;
using MapEditor.Extensions;
using Track;

namespace MapEditor.StateTracker.Segment
{
  public sealed class ChangeTrackSegment(TrackSegment segment) : IUndoable
  {
    private readonly TrackSegmentGhost _Old = new TrackSegmentGhost(segment.id);
    private readonly TrackSegmentGhost _New = new TrackSegmentGhost(segment.id, segment.a.id, segment.b.id, segment.priority, segment.speedLimit, segment.groupId!, segment.style, segment.trackClass);
    private bool _IsEditable = true;

    public ChangeTrackSegment Priority(int priority)
    {
      if (!_IsEditable) {
        throw new InvalidOperationException();
      }

      _New._priority = priority;
      return this;
    }

    public ChangeTrackSegment SpeedLimit(int speedLimit)
    {
      if (!_IsEditable) {
        throw new InvalidOperationException();
      }

      _New._speedLimit = speedLimit;
      return this;
    }

    public ChangeTrackSegment GroupId(string groupId)
    {
      if (!_IsEditable) {
        throw new InvalidOperationException();
      }

      _New._groupId = groupId;
      return this;
    }

    public ChangeTrackSegment Style(TrackSegment.Style style)
    {
      if (!_IsEditable) {
        throw new InvalidOperationException();
      }

      _New._style = style;
      return this;
    }

    public ChangeTrackSegment TrackClass(TrackClass trackClass)
    {
      if (!_IsEditable) {
        throw new InvalidOperationException();
      }

      _New._trackClass = trackClass;
      return this;
    }

    public ChangeTrackSegment Flip()
    {
      if (!_IsEditable) {
        throw new InvalidOperationException();
      }
      var a = segment.a;
      var b = segment.b;
      segment.a = b;
      segment.b = a;
      _New._a = b.id;
      _New._b = a.id;
      return this;
    }

    public void Apply()
    {
      _IsEditable = false;
      _Old.UpdateGhost(segment);
      _New.UpdateSegment(segment);
      EditorContext.PatchEditor!.AddOrUpdateSegment(segment);
    }

    public void Revert()
    {
      _Old.UpdateSegment(segment);
      EditorContext.PatchEditor!.AddOrUpdateSegment(segment);
    }

    public override string ToString()
    {
      return "ChangeTrackSegment: " + segment.id;
    }
  }
}
