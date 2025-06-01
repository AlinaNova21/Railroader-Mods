using System;
using System.Linq;
using Helpers;
using MapEditor.Dialogs;
using MapEditor.StateTracker.Node;
using UnityEngine;

namespace MapEditor.Managers
{
  public static class SceneryManager
  {

    public static void Move(Direction direction, SceneryAssetInstance? scenery = null)
    {
      scenery ??= EditorContext.SelectedScenery;
      if (scenery == null) {
        return;
      }
      var Scaling = EditorContext.Scaling.Movement / 100f;
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
      if (scenery == null) {
        return;
      }
      var scaling = EditorContext.Scaling.Rotation / 100f;
      EditorContext.ChangeManager.AddChange(new ChangeScenery(scenery).Rotate(scenery.transform.localEulerAngles + offset * scaling));
    }

    public static void Model(string value, SceneryAssetInstance? scenery = null)
    {
      scenery ??= EditorContext.SelectedScenery;
      if (scenery == null || scenery.identifier == value) {
        return;
      }
      EditorContext.ChangeManager.AddChange(new ChangeScenery(scenery).Model(value));
    }

    public static void AddScenery(Vector3 position)
    {
      var lid = EditorContext.SceneryIdGenerator.Next()!;
      EditorContext.ChangeManager.AddChange(new CreateScenery(lid, position, Vector3.zero, Vector3.one, "barn"));
      EditorContext.SelectedScenery = GameObject.FindObjectsOfType<SceneryAssetInstance>().FirstOrDefault(x => x.name == lid);
      Rebuild();
    }

    public static void RemoveScenery(SceneryAssetInstance? scenery = null)
    {
      scenery ??= EditorContext.SelectedScenery;
      EditorContext.SelectedScenery = null;

      if (scenery == null) {
        return;
      }
      EditorContext.ChangeManager.AddChange(new DeleteScenery(scenery));

      Rebuild();
    }


    #region Rotation

    public static void CopyRotation()
    {
      var obj = EditorContext.SelectedScenery;
      if (obj == null)
        return;
      Clipboard.Vector3 = obj.transform.localEulerAngles;
    }

    public static void PasteRotation()
    {
      var obj = EditorContext.SelectedScenery;
      if (obj == null)
        return;
      var vector = Clipboard.Vector3;
      EditorContext.ChangeManager.AddChange(new ChangeScenery(obj).Rotate(vector));
    }

    #endregion

    #region Elevation

    public static void CopyElevation()
    {
      var obj = EditorContext.SelectedScenery;
      if (obj == null)
        return;
      Clipboard.Set(obj.transform.localPosition.y);
    }

    public static void PasteElevation()
    {
      var obj = EditorContext.SelectedScenery;
      if (obj == null)
        return;
      EditorContext.ChangeManager.AddChange(new ChangeScenery(obj).Move(obj.transform.localPosition.x, Clipboard.Get<float>(), obj.transform.localPosition.z));
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
}
