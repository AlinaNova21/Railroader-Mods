using System;
using Helpers;
using UnityEngine;

namespace MapEditor.StateTracker.Node;

public sealed class ChangeScenery : IUndoable
{

  private readonly SceneryAssetInstance _Scenery;
  private readonly SceneryGhost _Old;
  private readonly SceneryGhost _New;
  private bool _IsEditable;

  public ChangeScenery(SceneryAssetInstance scenery)
  {
    _Scenery = scenery;
    _Old = new SceneryGhost(scenery.name);
    _New = new SceneryGhost(scenery.name, scenery.transform.localPosition, scenery.transform.localEulerAngles, scenery.transform.localScale, scenery.identifier);
    _New.UpdateGhost(scenery);
    _IsEditable = true;
  }

  public ChangeScenery Move(Vector3 newPosition)
  {
    if (!_IsEditable) {
      throw new InvalidOperationException();
    }

    _New._Position = newPosition;
    return this;
  }

  public ChangeScenery Move(float? x = null, float? y = null, float? z = null)
  {
    if (!_IsEditable) {
      throw new InvalidOperationException();
    }

    _New._Position = new Vector3(x ?? _New._Position.x, y ?? _New._Position.y, z ?? _New._Position.z);
    return this;
  }

  public ChangeScenery Rotate(Vector3 newRotation)
  {
    if (!_IsEditable) {
      throw new InvalidOperationException();
    }

    _New._Rotation = newRotation;
    return this;
  }

  public ChangeScenery Scale(Vector3 newScale)
  {
    if (!_IsEditable) {
      throw new InvalidOperationException();
    }
    _New._Scale = newScale;
    return this;
  }

  public ChangeScenery Model(string value)
  {
    if (!_IsEditable) {
      throw new InvalidOperationException();
    }

    _New._Model = value;
    return this;
  }

  public void Apply()
  {
    _IsEditable = false;
    _Old.UpdateGhost(_Scenery);
    _New.UpdateScenery(_Scenery);
    EditorContext.PatchEditor!.AddOrUpdateScenery(_Scenery.name, _Scenery.identifier, _Scenery.transform.localPosition, _Scenery.transform.localEulerAngles, _Scenery.transform.localScale);
  }

  public void Revert()
  {
    _Old.UpdateScenery(_Scenery);
    EditorContext.PatchEditor!.AddOrUpdateScenery(_Scenery.name, _Scenery.identifier, _Scenery.transform.localPosition, _Scenery.transform.localEulerAngles, _Scenery.transform.localScale);
  }

  public override string ToString()
  {
    return "ChangeScenery: " + _Scenery.name;
  }
}
