using System.Collections.Generic;

namespace MapEditor.StateTracker
{
    public class ChangeManager
    {
      private Stack<IUndoable> _undoStack = new Stack<IUndoable>();
      private Stack<IUndoable> _redoStack = new Stack<IUndoable>();

      public void AddChange(IUndoable change)
      {
          change.Apply();
          _undoStack.Push(change);
          _redoStack.Clear();
      }

      public void Undo()
      {
          if (_undoStack.Count == 0)
          {
              return;
          }

          var change = _undoStack.Pop();
          change.Revert();
          _redoStack.Push(change);
      }

      public void Redo()
      {
          if (_redoStack.Count == 0)
          {
              return;
          }

          var change = _redoStack.Pop();
          change.Apply();
          _undoStack.Push(change);
      }
    }
}