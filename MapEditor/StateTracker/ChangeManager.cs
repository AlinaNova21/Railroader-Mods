using System.Collections.Generic;
using Serilog;

namespace MapEditor.StateTracker
{
  public class ChangeManager
  {

    private readonly ILogger _logger = Log.ForContext(typeof(ChangeManager))!;
    private readonly Stack<IUndoable> _undoStack = new Stack<IUndoable>();
    private readonly Stack<IUndoable> _redoStack = new Stack<IUndoable>();

    public void AddChange(IUndoable change)
    {
      _logger.Information("Change: " + change);
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
