namespace MapEditor.StateTracker
{
  public interface IUndoable
  {

    void Apply();
    void Revert();

  }
}
