using System;
using MapEditor.StateTracker.Generic;
using UnityEngine;

namespace MapEditor.StateTracker.Node
{
  public sealed class CreateObject : IUndoable
  {
    private readonly string _id;
    private readonly ObjectGhost _ghost;

    public CreateObject(Type type, string id)
    {
      _id = id;
      _ghost = new ObjectGhost(type, id);
      _ghost.Position = Vector3.zero;
    }

    public CreateObject(Type type, string id, Vector3 position)
    {
      _id = id;
      _ghost = new ObjectGhost(type, id);
      _ghost.Position = position;
    }

    public void Apply()
    {
      _ghost.Create();
    }

    public void Revert()
    {
      _ghost.Destroy();
    }

    public override string ToString()
    {
      return "Create: " + _id;
    }

  }
}
