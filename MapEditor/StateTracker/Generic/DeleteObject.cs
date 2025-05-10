using MapEditor.Managers;
using MapEditor.StateTracker.Generic;

namespace MapEditor.StateTracker.Node
{
  public sealed class DeleteObject(IEditableObject obj) : IUndoable
  {

    private readonly string _Id = obj.Id;
    private ObjectGhost? _Ghost;

    public void Apply()
    {
      _Ghost = new ObjectGhost(_Id);
      //_Ghost.Destroy();
    }

    public void Revert()
    {
      //_Ghost!.Create();
    }

    public override string ToString()
    {
      return "DeleteObject: " + _Id;
    }

  }
}
