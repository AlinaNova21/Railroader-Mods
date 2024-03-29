using System.Linq;

namespace MapEditor.StateTracker
{
    public class CompoundChange : IUndoable
    {
        private readonly IUndoable[] _changes;

        public CompoundChange(params IUndoable[] changes)
        {
            _changes = changes;
        }

        public void Apply()
        {
            foreach (var change in _changes)
            {
                change.Apply();
            }
        }

        public void Revert()
        {
            foreach (var change in _changes.Reverse())
            {
                change.Revert();
            }
        }
    }
}