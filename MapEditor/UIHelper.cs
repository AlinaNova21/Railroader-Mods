using AlinasMapMod.MapEditor;
using MapEditor.Managers;
using MapEditor.StateTracker.Node;
using UnityEngine;

namespace MapEditor
{
  internal class UIHelper : IEditorUIHelper
  {
    private static UIHelper? _instance;
    public static UIHelper Instance
    {
      get {
        if (_instance == null) {
          _instance = new UIHelper();
        }
        return _instance;
      }
    }
    public Vector3 GetPosition()
    {
      var obj = EditorContext.SelectedObject as ITransformableObject;
      if (obj == null)
        return Vector3.zero;
      return obj.Position;
    }
    public Vector3 GetRotation()
    {
      var obj = EditorContext.SelectedObject as ITransformableObject;
      if (obj == null)
        return Vector3.zero;
      return obj.Rotation;
    }
    public void SetPosition(Vector3 pos)
    {
      var obj = EditorContext.SelectedObject as ITransformableObject;
      if (obj == null)
        return;
      EditorContext.ChangeManager.AddChange(new TransformObject(obj).Move(pos));
    }
    public void SetRotation(Vector3 rot)
    {
      var obj = EditorContext.SelectedObject as ITransformableObject;
      if (obj == null)
        return;
      EditorContext.ChangeManager.AddChange(new TransformObject(obj).Rotate(rot));
    }
    public void SetProperty(string propertyName, object value)
    {
      ObjectManager.SetProperty(propertyName, value);
    }
    public object GetProperty(string propertyName)
    {
      var obj = EditorContext.SelectedObject as IEditableObject;
      if (obj == null)
        return null!;
      return obj.GetProperty(propertyName);
    }
  }
}
