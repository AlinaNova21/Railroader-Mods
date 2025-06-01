using System;
using AlinasMapMod.Loaders;
using MapEditor.Dialogs;
using MapEditor.StateTracker.Node;
using UnityEngine;

namespace MapEditor.Managers;

public static class LoaderManager
{

  public static void Move(Direction direction, LoaderInstance? loader = null)
  {
    loader ??= EditorContext.SelectedLoader;
    if (loader == null) {
      return;
    }

    var Scaling = EditorContext.Scaling.Movement / 100f;

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

  public static void Rotate(Vector3 offset, LoaderInstance? loader = null)
  {
    loader ??= EditorContext.SelectedLoader;
    if (loader == null) {
      return;
    }
    var scaling = EditorContext.Scaling.Rotation / 100f;
    EditorContext.ChangeManager.AddChange(new ChangeLoader(loader).Rotate(loader.transform.localEulerAngles + (offset * scaling)));
  }

  public static void Prefab(string value, LoaderInstance? loader = null)
  {
    loader ??= EditorContext.SelectedLoader;
    if (loader == null || loader.Prefab == value) {
      return;
    }
    EditorContext.ChangeManager.AddChange(new ChangeLoader(loader).Prefab(value));
  }

  public static void Industry(string value, LoaderInstance? loader = null)
  {
    loader ??= EditorContext.SelectedLoader;
    if (loader == null || loader.Industry == value) {
      return;
    }
    EditorContext.ChangeManager.AddChange(new ChangeLoader(loader).Industry(value));
  }

  public static void AddLoader(Vector3 position)
  {
    var lid = EditorContext.LoaderIdGenerator.Next()!;
    EditorContext.ChangeManager.AddChange(new CreateLoader(lid, position, Vector3.zero, "", ""));
    EditorContext.SelectedLoader = LoaderInstance.FindById(lid);
    //var newNode = Graph.Shared.GetNode(nid);
    //EditorContext.SelectedNode = newNode;
    Rebuild();
  }

  public static void RemoveLoader(LoaderInstance? loader = null)
  {
    loader ??= EditorContext.SelectedLoader;
    EditorContext.SelectedLoader = null;

    if (loader == null) {
      return;
    }
    EditorContext.ChangeManager.AddChange(new DeleteLoader(loader));

    Rebuild();
  }

  #region Rotation

  public static void CopyNodeRotation()
  {
    var node = EditorContext.SelectedNode!;
    Clipboard.Vector3 = node.transform.localEulerAngles;
  }

  public static void PasteNodeRotation()
  {
    var node = EditorContext.SelectedNode!;
    EditorContext.ChangeManager.AddChange(new ChangeTrackNode(node).Rotate(Clipboard.Vector3));

    Rebuild();
  }

  #endregion

  #region Elevation

  public static void CopyNodeElevation()
  {
    var node = EditorContext.SelectedNode!;
    Clipboard.Set(node.transform.localPosition.y);
  }

  public static void PasteNodeElevation()
  {
    var node = EditorContext.SelectedNode!;
    EditorContext.ChangeManager.AddChange(new ChangeTrackNode(node).Move(y: Clipboard.Get<float>()));

    Rebuild();
  }

  #endregion

  private static void Rebuild()
  {
    var last = EditorContext.ChangeManager.LastChange;
    if (last == null) {
      return;
    }
  }
}
