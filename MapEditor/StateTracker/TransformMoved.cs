using System;
using UnityEngine;

namespace MapEditor.StateTracker
{
  [Obsolete("Replaced by as MoveTransform")]
  public class TransformMoved : IUndoable
  {

    private readonly UnityEngine.Transform _transform;
    private readonly Vector3 _oldPosition;
    private readonly Vector3 _newPosition;

    public TransformMoved(UnityEngine.Transform transform, Vector3 newPosition)
    {
      _transform = transform;
      _oldPosition = transform.localPosition;
      _newPosition = newPosition;
    }

    public void Apply()
    {
      _transform.localPosition = _newPosition;
    }

    public void Revert()
    {
      _transform.localPosition = _oldPosition;
    }

  }
}
