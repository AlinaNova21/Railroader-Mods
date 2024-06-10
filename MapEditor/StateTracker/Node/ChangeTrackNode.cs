using System;
using MapEditor.Extensions;
using Track;
using UnityEngine;

namespace MapEditor.StateTracker.Node
{
  public sealed class ChangeTrackNode : IUndoable
  {

    private readonly TrackNode _node;
    private readonly TrackNodeGhost _old;
    private readonly TrackNodeGhost _new;
    private bool _isEditable;

    public ChangeTrackNode(TrackNode node)
    {
      _node = node;
      _old = new TrackNodeGhost(node.id!);
      _new = new TrackNodeGhost(node.id);
      _new.UpdateGhost(node);
      _isEditable = true;
    }

    public ChangeTrackNode Move(Vector3 newPosition)
    {
      if (!_isEditable)
      {
        throw new InvalidOperationException();
      }

      _new._position = newPosition;
      return this;
    }

    public ChangeTrackNode Move(float? x = null, float? y = null, float? z = null)
    {
      if (!_isEditable)
      {
        throw new InvalidOperationException();
      }

      _new._position = new Vector3(x ?? _new._position.x, y ?? _new._position.y, z ?? _new._position.z);
      return this;
    }

    public ChangeTrackNode Rotate(Vector3 newRotation)
    {
      if (!_isEditable)
      {
        throw new InvalidOperationException();
      }

      _new._rotation = newRotation;
      return this;
    }

    public ChangeTrackNode FlipSwitchStand()
    {
      if (!_isEditable)
      {
        throw new InvalidOperationException();
      }

      _new._flipSwitchStand = !_new._flipSwitchStand;
      return this;
    }

    public void Apply()
    {
      _isEditable = false;
      _old.UpdateGhost(_node);
      _new.UpdateNode(_node);
      EditorContext.PatchEditor.AddOrUpdateNode(_node);
      Graph.Shared.OnNodeDidChange(_node);
    }

    public void Revert()
    {
      _old.UpdateNode(_node);
      EditorContext.PatchEditor.AddOrUpdateNode(_node);
      Graph.Shared.OnNodeDidChange(_node);
    }

    public override string ToString()
    {
      return "ChangeTrackNode: " + _node.id;
    }

  }
}
