using System;
using MapEditor.Extensions;
using Track;
using UnityEngine;

namespace MapEditor.StateTracker.Node
{
  public sealed class ChangeTrackNode : IUndoable
  {

    private readonly TrackNode _Node;
    private readonly TrackNodeGhost _Old;
    private readonly TrackNodeGhost _New;
    private bool _IsEditable;

    public ChangeTrackNode(TrackNode node)
    {
      _Node = node;
      _Old = new TrackNodeGhost(node.id);
      _New = new TrackNodeGhost(node.id, node.transform.localPosition, node.transform.localEulerAngles, node.flipSwitchStand);
      _New.UpdateGhost(node);
      _IsEditable = true;
    }

    public ChangeTrackNode Move(Vector3 newPosition)
    {
      if (!_IsEditable) {
        throw new InvalidOperationException();
      }

      _New._Position = newPosition;
      return this;
    }

    public ChangeTrackNode Move(float? x = null, float? y = null, float? z = null)
    {
      if (!_IsEditable) {
        throw new InvalidOperationException();
      }

      _New._Position = new Vector3(x ?? _New._Position.x, y ?? _New._Position.y, z ?? _New._Position.z);
      return this;
    }

    public ChangeTrackNode Rotate(Vector3 newRotation)
    {
      if (!_IsEditable) {
        throw new InvalidOperationException();
      }

      _New._Rotation = newRotation;
      return this;
    }

    public ChangeTrackNode FlipSwitchStand(bool value)
    {
      if (!_IsEditable) {
        throw new InvalidOperationException();
      }

      _New._FlipSwitchStand = value;
      return this;
    }

    public void Apply()
    {
      _IsEditable = false;
      _Old.UpdateGhost(_Node);
      _New.UpdateNode(_Node);
      EditorContext.PatchEditor!.AddOrUpdateNode(_Node);
      Graph.Shared.OnNodeDidChange(_Node);
    }

    public void Revert()
    {
      _Old.UpdateNode(_Node);
      EditorContext.PatchEditor!.AddOrUpdateNode(_Node);
      Graph.Shared.OnNodeDidChange(_Node);
    }

    public override string ToString()
    {
      return "ChangeTrackNode: " + _Node.id;
    }

  }
}
