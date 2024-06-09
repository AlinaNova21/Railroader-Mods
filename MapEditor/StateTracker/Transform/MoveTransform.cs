namespace MapEditor.StateTracker.Transform {
  using JetBrains.Annotations;
  using UnityEngine;

  public sealed class MoveTransform : IUndoable {

    private readonly Transform _transform;
    private readonly Vector3 _oldPosition;
    private readonly Vector3 _newPosition;

    public MoveTransform(Transform transform, Vector3 newPosition) {
      _transform = transform;
      _oldPosition = transform.localPosition;
      _newPosition = newPosition;
    }

    public void Apply() {
      _transform.localPosition = _newPosition;
    }

    public void Revert() {
      _transform.localPosition = _oldPosition;
    }

  }
}
