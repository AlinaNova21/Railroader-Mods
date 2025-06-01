using AlinasMapMod.MapEditor;
using MapEditor.StateTracker.Generic;

namespace MapEditor.StateTracker.Node
{
  public sealed class DeleteObject(IEditableObject obj) : IUndoable
  {

    private readonly string _Id = obj.Id;
    private ObjectGhost _Ghost = new ObjectGhost(obj.GetType(), obj.Id);

    public void Apply()
    {
      _Ghost.Destroy();
    }

    public void Revert()
    {
      _Ghost.Create();
    }

    public override string ToString()
    {
      return "DeleteObject: " + _Id;
    }

  }
}
