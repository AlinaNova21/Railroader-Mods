using System;
using UnityEngine;

namespace MapEditor.StateTracker
{
  [Obsolete("Replaced by as RotateTransform")]
  public class TransformRotated : IUndoable
  {

    private readonly UnityEngine.Transform _transform;
    private readonly Quaternion _oldRotation;
    private readonly Quaternion _newRotation;

    public TransformRotated(UnityEngine.Transform transform, Quaternion newRotation)
    {
      _transform = transform;
      _oldRotation = transform.localRotation;
      _newRotation = newRotation;
    }

    public void Apply()
    {
      _transform.localRotation = _newRotation;
    }

    public void Revert()
    {
      _transform.localRotation = _oldRotation;
    }

  }
}
