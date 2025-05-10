
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
        var val = obj.GetProperty<object>(property);
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
  }
}
