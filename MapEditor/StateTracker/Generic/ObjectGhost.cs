
using System;
using System.Collections.Generic;
using AlinasMapMod.MapEditor;
using UnityEngine;

namespace MapEditor.StateTracker.Generic;

public class ObjectGhost
{
  public Vector3 Position { get; set; }
  public Vector3 Rotation { get; set; }
  public Vector3 Scale { get; set; } = Vector3.one;
  public Dictionary<string, object> Properties { get; } = new Dictionary<string, object>();

  private string Id { get; set; }
  private Type Type { get; set; }

  public ObjectGhost(string id)
  {
    Id = id;
    Type = typeof(GameObject);
  }

  public ObjectGhost(Type type, string id)
  {
    Type = type;
    Id = id;
  }

  public void UpdateGhost(IEditableObject obj)
  {
    if (obj is ITransformableObject transformable) {
      if (transformable.CanMove) {
        Position = transformable.Position;
      }
      if (transformable.CanRotate) {
        Rotation = transformable.Rotation;
      }
      if (transformable.CanScale) {
        Scale = transformable.Scale;
      }
    }
    foreach (var property in obj.Properties) {
      var val = obj.GetProperty(property);
      if (val != null) {
        Properties[property] = val;
      }
    }
    obj.Save(EditorContext.PatchEditor!);
  }

  public void UpdateObject(IEditableObject obj)
  {
    foreach (var property in obj.Properties) {
      if (Properties.ContainsKey(property)) {
        obj.SetProperty(property, Properties[property]);
      }
    }
    if (obj is ITransformableObject transformable) {
      if (transformable.CanMove) {
        transformable.Position = Position;
      }
      if (transformable.CanRotate) {
        transformable.Rotation = Rotation;
      }
      if (transformable.CanScale) {
        transformable.Scale = Scale;
      }
      EditorContext.AttachUiHelper(transformable);
    }
  }

  public void Create()
  {
    var parent = GameObject.Find("World");
    var go = new GameObject(Id);
    go.transform.parent = parent.transform;
    var obj = (IEditableObject)go.AddComponent(Type);
    obj.Id = Id;
    UpdateObject(obj);
    obj.Save(EditorContext.PatchEditor!);
    if (obj is ITransformableObject transformable) {
      EditorContext.AttachUiHelper(transformable);
    }
  }

  public void Destroy()
  {
    var obj = GameObject.Find(Id);
    if (obj != null) {
      var editable = obj.GetComponent<IEditableObject>();
      UpdateGhost(editable);
      if (!editable.CanDestroy) return;
      editable.Destroy(EditorContext.PatchEditor!);
      UnityEngine.Object.Destroy(obj);
    }
  }
}
