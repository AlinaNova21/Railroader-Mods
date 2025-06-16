using System;
using AlinasMapMod.MapEditor;
using MapEditor.StateTracker.Generic;
using UnityEngine;

namespace MapEditor.StateTracker.Node;

public sealed class TransformObject : IUndoable
{
  private readonly ITransformableObject _Object;
  private readonly ObjectGhost _Old;
  private readonly ObjectGhost _New;
  private bool _IsEditable;

  public TransformObject(ITransformableObject obj)
  {
    _Object = obj;
    _Old = new ObjectGhost(obj.Id);
    _New = new ObjectGhost(obj.Id);
    _New.UpdateGhost(obj);
    _IsEditable = true;
  }

  public TransformObject Move(Vector3 newPosition)
  {
    if (!_IsEditable || !_Object.CanMove) {
      throw new InvalidOperationException();
    }

    Serilog.Log.Logger.Debug("TransformObject.Move: {0} to {1}", _Object.Position, newPosition);
    _New.Position = newPosition;
    return this;
  }

  public TransformObject Move(float? x = null, float? y = null, float? z = null)
  {
    return Move(new Vector3(x ?? _New.Position.x, y ?? _New.Position.y, z ?? _New.Position.z));
  }

  public TransformObject Rotate(Vector3 newRotation)
  {
    if (!_IsEditable || !_Object.CanRotate) {
      throw new InvalidOperationException();
    }

    _New.Rotation = newRotation;
    return this;
  }

  public TransformObject Scale(Vector3 newScale)
  {
    if (!_IsEditable || !_Object.CanScale) {
      throw new InvalidOperationException();
    }
    _New.Scale = newScale;
    return this;
  }

  public void Apply()
  {
    _IsEditable = false;
    _Old.UpdateGhost(_Object);
    _New.UpdateObject(_Object);
    _Object.Save(EditorContext.PatchEditor!);
  }

  public void Revert()
  {
    _Old.UpdateObject(_Object);
    _Object.Save(EditorContext.PatchEditor!);
  }

  public override string ToString()
  {
    return "TransformObject: " + _Object.Id;
  }
}
