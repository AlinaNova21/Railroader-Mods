using System;
using System.Linq;
using System.Runtime.Remoting;
using AlinasMapMod.MapEditor;
using MapEditor.Dialogs;
using MapEditor.StateTracker;
using MapEditor.StateTracker.Generic;
using MapEditor.StateTracker.Node;
using RLD;
using UnityEngine;
using UnityEngine.UI;

namespace MapEditor.Managers;

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
    var changes = EditorContext.SelectedObjects
      .OfType<ITransformableObject>()
      .Select(o => new TransformObject(o).Move(o.Position + vector));
    IUndoable change = changes.Count() > 1
      ? new CompoundChange(changes)
      : changes.FirstOrDefault();
    EditorContext.ChangeManager.AddChange(change);
  }

  public static void Rotate(Vector3 offset, ITransformableObject? obj = null)
  {

    obj ??= EditorContext.SelectedObject as ITransformableObject;
    if (obj == null) {
      return;
    }
    var scaling = EditorContext.Scaling.Rotation / 100f;
    var angle = offset * scaling;
    var rotateAround = (ITransformableObject o) => (Quaternion.Euler(angle) * (o.Position - obj.Position)) + obj.Position;
    var changes = EditorContext.SelectedObjects
      .OfType<ITransformableObject>()
      .Select(o => new TransformObject(o).Move(rotateAround(o)).Rotate(o.Rotation + angle));
    IUndoable change = changes.Count() > 1
      ? new CompoundChange(changes)
      : changes.FirstOrDefault();
    EditorContext.ChangeManager.AddChange(change);
  }

  public static void Scale(Vector3 offset, ITransformableObject? obj = null)
  {
    obj ??= EditorContext.SelectedObject as ITransformableObject;
    if (obj == null) {
      return;
    }
    var scaling = EditorContext.Scaling.Scale / 100f;
    var scaleAmt = offset * scaling;

    var changes = EditorContext.SelectedObjects
      .OfType<ITransformableObject>()
      .Select(o => new TransformObject(o).Move(o.Position + (o.Position - obj.Position)).Scale(o.Transform.localScale + scaleAmt));
    IUndoable change = changes.Count() > 1
      ? new CompoundChange(changes)
      : changes.FirstOrDefault();
    EditorContext.ChangeManager.AddChange(change);
    // TODO: implement scaling in ITransformableObject
  }

  public static void Align(Vector3 dir, ITransformableObject? obj = null)
  {
    obj ??= EditorContext.SelectedObject as ITransformableObject;
    if (obj == null) {
      return;
    }
    if (!obj.CanMove) {
      return;
    }

    var direction = dir.x != 0 ? obj.Transform.right : dir.z != 0 ? obj.Transform.forward : Vector3.zero;
    
    Vector3 align(ITransformableObject o)
    {
      var v3 = o.Position - obj.Position;
      var t = Vector3.Dot(v3, direction);
      var pt = obj.Position + (direction * t);
      return pt;
    }

    var changes = EditorContext.SelectedObjects
      .OfType<ITransformableObject>()
      .Skip(1) // Skip the first object, which is the one we are aligning to
      .Select(o => new TransformObject(o).Move(align(o)));
    IUndoable change = changes.Count() > 1
      ? new CompoundChange(changes)
      : changes.FirstOrDefault();
    EditorContext.ChangeManager.AddChange(change);
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
    var changes = EditorContext.SelectedObjects
      .Select(o => new DeleteObject(o))
      .ToList();
    IUndoable change = changes.Count() > 1
      ? new CompoundChange(changes)
      : changes.FirstOrDefault();
    EditorContext.ChangeManager.AddChange(change);
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
