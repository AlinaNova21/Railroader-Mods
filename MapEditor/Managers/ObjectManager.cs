using System;
using System.Linq;
using AlinasMapMod.MapEditor;
using MapEditor.Dialogs;
using MapEditor.StateTracker.Generic;
using MapEditor.StateTracker.Node;
using UnityEngine;

namespace MapEditor.Managers
{
  internal class ObjectManager
  {
    private static Serilog.ILogger logger = Serilog.Log.ForContext<ObjectManager>();
    public static void Move(Direction direction, ITransformableObject? obj = null)
    {
      obj ??= EditorContext.SelectedObject as ITransformableObject;
      if (obj == null) {
        return;
      }

      if (!obj.CanMove) {
        return;
      }
      var scaling = EditorContext.Scaling.Movement / 100f;
      var pos = obj.Position;
      var vector =
        direction switch
        {
          Direction.up => obj.Transform.up * scaling,
          Direction.down => obj.Transform.up * -scaling,
          Direction.left => obj.Transform.right * -scaling,
          Direction.right => obj.Transform.right * scaling,
          Direction.forward => obj.Transform.forward * scaling,
          Direction.backward => obj.Transform.forward * -scaling,
          _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null!)
        };
      EditorContext.ChangeManager.AddChange(new TransformObject(obj).Move(obj.Position + vector));
    }

    public static void Rotate(Vector3 offset, ITransformableObject? obj = null)
    {

      obj ??= EditorContext.SelectedObject as ITransformableObject;
      if (obj == null) {
        return;
      }
      var scaling = EditorContext.Scaling.Rotation / 100f;
      EditorContext.ChangeManager.AddChange(new TransformObject(obj).Rotate(obj.Rotation + offset * scaling));
    }

    public static void Scale(Vector3 offset, ITransformableObject? obj = null)
    {
      obj ??= EditorContext.SelectedObject as ITransformableObject;
      if (obj == null) {
        return;
      }
      var scaling = EditorContext.Scaling.Scale / 100f;
      // TODO: implement scaling in ITransformableObject
      EditorContext.ChangeManager.AddChange(new TransformObject(obj).Scale(obj.Transform.localScale + offset * scaling));
    }

    public static void SetProperty(string property, object value, IEditableObject? obj = null)
    {
      obj ??= EditorContext.SelectedObject;
      if (obj == null) {
        return;
      }
      EditorContext.ChangeManager.AddChange(new ChangeProperty(obj).SetProperty(property, value));
    }

    public static void Create(Type type, string id, Vector3 position)
    {
      EditorContext.ChangeManager.AddChange(new CreateObject(type, id, position));
      EditorContext.SelectedObject = GameObject.FindObjectsOfType<MonoBehaviour>()
          .Where(o => o is IEditableObject)
          .Select(o => o as IEditableObject)
          .FirstOrDefault(x => x!.Id == id);
    }

    public static void Remove(IEditableObject? obj = null)
    {
      obj ??= EditorContext.SelectedObject;
      if (obj == null) {
        return;
      }
      EditorContext.SelectedObject = null;
      EditorContext.ChangeManager.AddChange(new DeleteObject(obj));
    }

    #region Rotation

    public static void CopyRotation()
    {
      var obj = EditorContext.SelectedObject! as ITransformableObject;
      if (obj == null)
        return;
      Clipboard.Vector3 = obj.Rotation;
    }

    public static void PasteRotation()
    {
      var obj = EditorContext.SelectedObject! as ITransformableObject;
      if (obj == null)
        return;
      var vector = Clipboard.Vector3;
      EditorContext.ChangeManager.AddChange(new TransformObject(obj).Rotate(vector));
    }

    #endregion

    #region Elevation

    public static void CopyElevation()
    {
      var obj = EditorContext.SelectedObject! as ITransformableObject;
      if (obj == null)
        return;
      Clipboard.Set(obj.Position.y);
    }

    public static void PasteElevation()
    {
      var obj = EditorContext.SelectedObject! as ITransformableObject;
      if (obj == null)
        return;
      EditorContext.ChangeManager.AddChange(new TransformObject(obj).Move(obj.Position.x, Clipboard.Get<float>(), obj.Position.z));
    }

    #endregion
  }
}
