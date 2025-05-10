using System;
using AlinasMapMod.Definitions;
using MapEditor.Extensions;
using Newtonsoft.Json.Linq;
using Track;
using UnityEngine;
using static AlinasMapMod.LoaderBuilder;

namespace MapEditor.StateTracker.Node
{
  public sealed class ChangeLoader : IUndoable
  {

    private readonly CustomLoader _Loader;
    private readonly LoaderGhost _Old;
    private readonly LoaderGhost _New;
    private bool _IsEditable;

    public ChangeLoader(CustomLoader loader)
    {
      _Loader = loader;
      _Old = new LoaderGhost(loader.id);
      _New = new LoaderGhost(loader.id, loader.transform.localPosition, loader.transform.localEulerAngles, loader.config.Prefab, loader.config.Industry);
      _New.UpdateGhost(loader);
      _IsEditable = true;
    }

    public ChangeLoader Move(Vector3 newPosition)
    {
      if (!_IsEditable)
      {
        throw new InvalidOperationException();
      }

      _New._Position = newPosition;
      return this;
    }

    public ChangeLoader Move(float? x = null, float? y = null, float? z = null)
    {
      if (!_IsEditable)
      {
        throw new InvalidOperationException();
      }

      _New._Position = new Vector3(x ?? _New._Position.x, y ?? _New._Position.y, z ?? _New._Position.z);
      return this;
    }

    public ChangeLoader Rotate(Vector3 newRotation)
    {
      if (!_IsEditable)
      {
        throw new InvalidOperationException();
      }

      _New._Rotation = newRotation;
      return this;
    }

    public ChangeLoader Prefab(string value)
    {
      if (!_IsEditable)
      {
        throw new InvalidOperationException();
      }

      _New._Prefab = value;
      return this;
    }

    public ChangeLoader Industry(string value)
    {
      if (!_IsEditable)
      {
        throw new InvalidOperationException();
      }

      _New._Industry = value;
      return this;
    }

    

    public void Apply()
    {
      _IsEditable = false;
      _Old.UpdateGhost(_Loader);
      _New.UpdateLoader(_Loader);
      EditorContext.PatchEditor!.AddOrUpdateSpliney(_Loader.id, (data) => _New.GetSpliney());
    }

    public void Revert()
    {
      _Old.UpdateLoader(_Loader);
      EditorContext.PatchEditor!.AddOrUpdateSpliney(_Loader.id, (data) => _Old.GetSpliney());
    }

    public override string ToString()
    {
      return "ChangeLoader: " + _Loader.id;
    }

  }
}
