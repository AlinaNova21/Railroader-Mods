using UnityEngine;

namespace AlinasMapMod.MapEditor;

public interface IEditorUIHelper
{
  public Vector3 GetPosition();
  public Vector3 GetRotation();
  public void SetPosition(Vector3 pos);
  public void SetRotation(Vector3 rot);
  public void SetProperty(string propertyName, object value);
  public object GetProperty(string propertyName);
}
