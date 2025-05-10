using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using MapEditor.StateTracker.Node;
using UnityEngine;

namespace MapEditor.Managers
{
  public static class SceneryManager
  {

    public static void Move(Direction direction, SceneryAssetInstance? scenery = null)
    {
      scenery ??= EditorContext.SelectedScenery;
      if (scenery == null)
      {
        return;
      }

      var vector =
        direction switch
        {
          Direction.up => scenery.transform.up * Scaling,
          Direction.down => scenery.transform.up * -Scaling,
          Direction.left => scenery.transform.right * -Scaling,
          Direction.right => scenery.transform.right * Scaling,
          Direction.forward => scenery.transform.forward * Scaling,
          Direction.backward => scenery.transform.forward * -Scaling,
          _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null!)
        };

      EditorContext.ChangeManager.AddChange(new ChangeScenery(scenery).Move(scenery.transform.localPosition + vector));
    }

    public static void Rotate(Vector3 offset, SceneryAssetInstance? scenery = null)
    {
      scenery ??= EditorContext.SelectedScenery;
      if (scenery == null)
      {
        return;
      }

      EditorContext.ChangeManager.AddChange(new ChangeScenery(scenery).Rotate(scenery.transform.localEulerAngles + offset * Scaling));
    }

    public static void Model(string value, SceneryAssetInstance? scenery = null)
    {
      scenery ??= EditorContext.SelectedScenery;
      if (scenery == null || scenery.identifier == value)
      {
        return;
      }
      EditorContext.ChangeManager.AddChange(new ChangeScenery(scenery).Model(value));
    }

    #region Scaling

    public static float Scaling { get => NodeManager.Scaling; }

    #endregion
    public static void AddScenery(Vector3 position)
    {
      var lid = EditorContext.SceneryIdGenerator.Next()!;
      EditorContext.ChangeManager.AddChange(new CreateScenery(lid, position, Vector3.zero, Vector3.one, "barn"));
      EditorContext.SelectedScenery = GameObject.FindObjectsOfType<SceneryAssetInstance>().FirstOrDefault(x => x.name == lid);
      //var newNode = Graph.Shared.GetNode(nid);
      //EditorContext.SelectedNode = newNode;
      Rebuild();
    }

    public static void RemoveScenery(SceneryAssetInstance? scenery = null)
    {
      scenery ??= EditorContext.SelectedScenery;
      EditorContext.SelectedScenery = null;

      EditorContext.ChangeManager.AddChange(new DeleteScenery(scenery));

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
