using System;
using Track;
using UnityEngine;

namespace MapEditor.StateTracker
{
  [Obsolete("Replaced by as ChangeTrackNode")]
  public class TrackNodeChanged : IUndoable
  {

    private readonly TrackNode _node;
    private readonly Vector3 _oldPosition;
    private readonly Vector3 _newPosition;
    private readonly Vector3 _oldRotation;
    private readonly Vector3 _newRotation;
    private readonly bool _oldFlipSwitchStand;
    private readonly bool _newFlipSwitchStand;

    public TrackNodeChanged(TrackNode node, Vector3 newPosition, Vector3 newRoration, bool flipSwitchStand)
    {
      _node = node;
      _oldPosition = node.transform.localPosition;
      _newPosition = newPosition;
      _oldRotation = node.transform.localEulerAngles;
      _newRotation = newRoration;
      _oldFlipSwitchStand = node.flipSwitchStand;
      _newFlipSwitchStand = flipSwitchStand;
    }

    public void Apply()
    {
      _node.transform.localPosition = _newPosition;
      _node.transform.localEulerAngles = _newRotation;
      _node.flipSwitchStand = _newFlipSwitchStand;
      EditorContext.PatchEditor.AddOrUpdateNode(_node.id, _newPosition, _newRotation, _newFlipSwitchStand);
      Graph.Shared.OnNodeDidChange(_node);
    }

    public void Revert()
    {
      _node.transform.localPosition = _oldPosition;
      _node.transform.localEulerAngles = _oldRotation;
      _node.flipSwitchStand = _oldFlipSwitchStand;
      EditorContext.PatchEditor.AddOrUpdateNode(_node.id, _oldPosition, _oldRotation, _oldFlipSwitchStand);
      Graph.Shared.OnNodeDidChange(_node);
    }

  }
}
