using MapEditor.StateTracker.Generic;
using UnityEngine;

namespace MapEditor.StateTracker.Node
{
  public sealed class CreateObject : IUndoable
  {

    private readonly string _id;
    private readonly ObjectGhost _ghost;

    public CreateObject(string id)
    {
      _id = id;
      _ghost = new ObjectGhost(id);
    }

    public void Apply()
    {
      //_ghost.Create();
    }

    public void Revert()
    {
      //_ghost.Destroy();
    }

    public override string ToString()
    {
      return "Create: " + _id;
    }

  }
}
