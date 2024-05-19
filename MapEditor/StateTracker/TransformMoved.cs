using UnityEngine;

namespace MapEditor.StateTracker
{
    public class TransformMoved : IUndoable
    {
        private readonly Transform _transform;
        private readonly Vector3 _oldPosition;
        private readonly Vector3 _newPosition;

        public TransformMoved(Transform transform, Vector3 newPosition)
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