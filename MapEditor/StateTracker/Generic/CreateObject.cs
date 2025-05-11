using System;
using System.ComponentModel;
using MapEditor.StateTracker.Generic;

namespace MapEditor.StateTracker.Node
{
  public sealed class CreateObject<T> : IUndoable
    where T: Component, IEditableObject
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
      // Hack to call the generic Create method
      _ghost.GetType().GetMethod("Create").MakeGenericMethod(typeof(T)).Invoke(_ghost, null);
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
