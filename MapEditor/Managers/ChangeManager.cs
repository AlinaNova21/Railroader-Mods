using System.Collections.Generic;
using MapEditor.StateTracker;

namespace MapEditor.Managers
{
  public sealed class ChangeManager
  {

    private readonly Stack<IUndoable> _UndoStack = new Stack<IUndoable>();
    private readonly Stack<IUndoable> _RedoStack = new Stack<IUndoable>();

    public IUndoable? LastChange => _UndoStack.Count > 0 ? _UndoStack.Peek() : null;

    public int Count => _UndoStack.Count;

    public void AddChange(IUndoable change)
    {
      change.Apply();
      _UndoStack.Push(change);
      _RedoStack.Clear();
    }

    public void Undo()
    {
      if (_UndoStack.Count == 0) {
        return;
      }

      var change = _UndoStack.Pop()!;
      change.Revert();
      _RedoStack.Push(change);
    }

    public void Redo()
    {
      if (_RedoStack.Count == 0) {
        return;
      }

      var change = _RedoStack.Pop()!;
      change.Apply();
      _UndoStack.Push(change);
    }

    public void Clear()
    {
      _UndoStack.Clear();
      _RedoStack.Clear();
    }

    public void UndoAll()
    {
      while (_UndoStack.Count > 0) {
        var change = _UndoStack.Pop()!;
        change.Revert();
      }
    }

  }
}
