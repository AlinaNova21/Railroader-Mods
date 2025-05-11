
using System;
using System.Collections.Generic;
using MapEditor.Managers;
using UnityEngine;

namespace MapEditor.StateTracker.Generic
{
  public class ObjectGhost(string id)
  {
    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }
    public Dictionary<string, object> Properties { get; } = new Dictionary<string, object>();

    public void UpdateGhost(IEditableObject obj)
    {
      if (obj is ITransformableObject transformable)
      {
        Position = transformable.Position;
        Rotation = transformable.Rotation;
      }
      foreach (var property in obj.Properties)
      {
        var val = obj.GetProperty(property);
        if (val != null)
        {
          Properties[property] = val;
        }
      }
    }

    public void UpdateObject(IEditableObject obj)
    {
      if (obj is ITransformableObject transformable)
      {
        transformable.Position = Position;
        transformable.Rotation = Rotation;
      }
      foreach (var property in obj.Properties)
      {
        if (Properties.ContainsKey(property))
        {
          obj.SetProperty(property, Properties[property]);
        }
      }
    }

    public void Create<T>() where T : Component, IEditableObject
    {
      var parent = GameObject.Find("Large Scenery");
      var go = new GameObject(id);
      go.transform.parent = parent.transform;
      var obj = go.AddComponent<T>();
      obj.Id = id;
      UpdateObject(obj);
      obj.Save();
      if (obj is ITransformableObject transformable)
      {
        EditorContext.AttachUiHelper(transformable);
      }
    }

    public void Destroy()
    {
      var obj = GameObject.Find(id);
      if (obj != null)
      {
        UnityEngine.Object.Destroy(obj);
      }
    }
  }
}
