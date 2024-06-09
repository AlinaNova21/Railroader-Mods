using System;
using MapEditor.Extensions;
using Track;

namespace MapEditor.StateTracker.Segment
{
  public sealed class ChangeTrackSegment : IUndoable
  {

    private readonly TrackSegment _segment;
    private readonly TrackSegmentGhost _old;
    private readonly TrackSegmentGhost _new;
    private bool _isEditable = true;

    public ChangeTrackSegment(TrackSegment segment)
    {
      _segment = segment;
      _old = new TrackSegmentGhost(segment.id!);
      _new = new TrackSegmentGhost(segment.id, segment.a.id, segment.b.id);
    }

    public ChangeTrackSegment Priority(int priority)
    {
      if (!_isEditable)
      {
        throw new InvalidOperationException();
      }

      _new._priority = priority;
      return this;
    }

    public ChangeTrackSegment SpeedLimit(int speedLimit)
    {
      if (!_isEditable)
      {
        throw new InvalidOperationException();
      }

      _new._speedLimit = speedLimit;
      return this;
    }

    public ChangeTrackSegment GroupId(string GroupId)
    {
      if (!_isEditable)
      {
        throw new InvalidOperationException();
      }

      _new._groupId = GroupId;
      return this;
    }

    public ChangeTrackSegment Style(TrackSegment.Style style)
    {
      if (!_isEditable)
      {
        throw new InvalidOperationException();
      }

      _new._style = style;
      return this;
    }

    public ChangeTrackSegment TrackClass(TrackClass trackClass)
    {
      if (!_isEditable)
      {
        throw new InvalidOperationException();
      }

      _new._trackClass = trackClass;
      return this;
    }

    public void Apply()
    {
      _isEditable = false;
      _old.UpdateGhost(_segment);
      _new.UpdateSegment(_segment);
      EditorContext.Instance.PatchEditor.AddOrUpdateSegment(_segment);
    }

    public void Revert()
    {
      _old.UpdateSegment(_segment);
      EditorContext.Instance.PatchEditor.AddOrUpdateSegment(_segment);
    }

    public ChangeTrackSegment SetGroupId(string value)
    {
      _new._groupId = value;
      return this;
    }

    public override string ToString()
    {
      return "ChangeTrackSegment: " + _segment.id;
    }

  }
}
