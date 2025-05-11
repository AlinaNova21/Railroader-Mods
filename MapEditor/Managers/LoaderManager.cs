using System;
using System.Collections.Generic;
using System.Linq;
using MapEditor.StateTracker;
using MapEditor.StateTracker.Node;
using MapEditor.StateTracker.Segment;
using Track;
using UnityEngine;
using static AlinasMapMod.LoaderBuilder;

namespace MapEditor.Managers
{
  public static class LoaderManager
  {

    public static void Move(Direction direction, CustomLoader? loader = null)
    {
      loader ??= EditorContext.SelectedLoader;
      if (loader == null)
      {
        return;
      }

      var vector =
        direction switch
        {
          Direction.up => loader.transform.up * Scaling,
          Direction.down => loader.transform.up * -Scaling,
          Direction.left => loader.transform.right * -Scaling,
          Direction.right => loader.transform.right * Scaling,
          Direction.forward => loader.transform.forward * Scaling,
          Direction.backward => loader.transform.forward * -Scaling,
          _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null!)
        };

      EditorContext.ChangeManager.AddChange(new ChangeLoader(loader).Move(loader.transform.localPosition + vector));
    }

    public static void Rotate(Vector3 offset, CustomLoader? loader = null)
    {
      loader ??= EditorContext.SelectedLoader;
      if (loader == null)
      {
        return;
      }

      EditorContext.ChangeManager.AddChange(new ChangeLoader(loader).Rotate(loader.transform.localEulerAngles + offset * Scaling));
    }

    public static void Prefab(string value, CustomLoader? loader = null)
    {
      loader ??= EditorContext.SelectedLoader;
      if (loader == null || loader.config.Prefab == value)
      {
        return;
      }
      EditorContext.ChangeManager.AddChange(new ChangeLoader(loader).Prefab(value));
    }

    public static void Industry(string value, CustomLoader? loader = null)
    {
      loader ??= EditorContext.SelectedLoader;
      if (loader == null || loader.config.Industry == value)
      {
        return;
      }
      EditorContext.ChangeManager.AddChange(new ChangeLoader(loader).Industry(value));
    }

    #region Scaling

    public static float Scaling { get => NodeManager.Scaling; }

    #endregion

    public static void AddLoader(Vector3 position)
    {
      var lid = EditorContext.LoaderIdGenerator.Next()!;
      EditorContext.ChangeManager.AddChange(new CreateLoader(lid, position, Vector3.zero, "", ""));
      EditorContext.SelectedLoader = CustomLoader.FindById(lid);
      //var newNode = Graph.Shared.GetNode(nid);
      //EditorContext.SelectedNode = newNode;
      Rebuild();
    }

    public static void RemoveLoader(CustomLoader? loader = null)
    {
      loader ??= EditorContext.SelectedLoader;
      EditorContext.SelectedLoader = null;

      if (loader == null)
      {
        return;
      }
      EditorContext.ChangeManager.AddChange(new DeleteLoader(loader));

      Rebuild();
    }

    #region Rotation

    private static Vector3 _savedRotation = Vector3.forward;

    public static void CopyNodeRotation()
    {
      var node = EditorContext.SelectedNode!;
      _savedRotation = node.transform.localEulerAngles;
    }

    public static void PasteNodeRotation()
    {
      var node = EditorContext.SelectedNode!;
      EditorContext.ChangeManager.AddChange(new ChangeTrackNode(node).Rotate(_savedRotation));

      Rebuild();
    }

    #endregion

    #region Elevation

    private static float _savedElevation;

    public static void CopyNodeElevation()
    {
      var node = EditorContext.SelectedNode!;
      _savedElevation = node.transform.localPosition.y;
    }

    public static void PasteNodeElevation()
    {
      var node = EditorContext.SelectedNode!;
      EditorContext.ChangeManager.AddChange(new ChangeTrackNode(node).Move(y: _savedElevation));

      Rebuild();
    }

    #endregion

    private static void Rebuild(){
      var last = EditorContext.ChangeManager.LastChange;
      if (last == null) {
        return;
      }
    }
  }
}
