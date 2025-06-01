using System.Collections.Generic;
using System.Linq;

namespace MapEditor.StateTracker
{
  public sealed class CompoundChange : IUndoable
  {

    private readonly IUndoable[] _changes;

    public CompoundChange(params IUndoable[] changes)
    {
      _changes = changes;
    }

    public CompoundChange(IEnumerable<IUndoable> changes)
      : this(changes.ToArray())
    {
    }

    public void Apply()
    {
      foreach (var change in _changes) {
        change.Apply();
      }
    }

    public void Revert()
    {
      foreach (var change in _changes.Reverse()) {
        change.Revert();
      }
    }

  }
}
