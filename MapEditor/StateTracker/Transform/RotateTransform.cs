namespace MapEditor.StateTracker.Transform {
  using JetBrains.Annotations;
  using UnityEngine;

  public sealed class RotateTransform : IUndoable {

    private readonly Transform _transform;
    private readonly Quaternion _oldRotation;
    private readonly Quaternion _newRotation;

    public RotateTransform(Transform transform, Quaternion newRotation) {
      _transform = transform;
      _oldRotation = transform.localRotation;
      _newRotation = newRotation;
    }

    public void Apply() {
      _transform.localRotation = _newRotation;
    }

    public void Revert() {
      _transform.localRotation = _oldRotation;
    }

  }
}
