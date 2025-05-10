using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MapEditor.StateTracker.Node;
using UnityEngine;

namespace MapEditor.Managers
{
  internal class ObjectManager
  {
    public static void Move(Direction direction, ITransformableObject? obj = null)
    {
      obj ??= EditorContext.SelectedObject as ITransformableObject;
      if (obj == null)
      {
        return;
      }

      if (!obj.CanMove)
      {
        return;
      }

      var pos = obj.Position;
      var vector =
        direction switch
        {
          Direction.up => obj.Transform.up * Scaling,
          Direction.down => obj.Transform.up * -Scaling,
          Direction.left => obj.Transform.right * -Scaling,
          Direction.right => obj.Transform.right * Scaling,
          Direction.forward => obj.Transform.forward * Scaling,
          Direction.backward => obj.Transform.forward * -Scaling,
          _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null!)
        };

      EditorContext.ChangeManager.AddChange(new TransformObject(obj).Move(obj.Transform.localPosition + vector));
    }

    public static void Rotate(Vector3 offset, ITransformableObject? obj = null)
    {

      obj ??= EditorContext.SelectedObject as ITransformableObject;
      if (obj == null)
      {
        return;
      }

      EditorContext.ChangeManager.AddChange(new TransformObject(obj).Rotate(obj.Transform.localEulerAngles + offset * Scaling));
    }
    
    #region Scaling

    public static float Scaling { get; set; } = 1.0f;

    public static void MultiplyScaling()
    {
      if (Scaling <= 10)
      {
        Scaling *= 10;
      }
    }

    public static void DivideScaling()
    {
      if (Scaling > 0.01f)
      {
        Scaling /= 10;
      }
    }

    #endregion

    #region Rotation

    private static Vector3 _savedRotation = Vector3.forward;

    public static void CopyRotation()
    {
      var obj = EditorContext.SelectedObject! as ITransformableObject;
      if (obj == null)
        return;
      _savedRotation = obj.Rotation;
    }

    public static void PasteRotation()
    {
      var obj = EditorContext.SelectedObject! as ITransformableObject;
      if (obj == null)
        return;
      EditorContext.ChangeManager.AddChange(new TransformObject(obj).Rotate(_savedRotation));
    }

    #endregion

    #region Elevation

    private static float _savedElevation;

    public static void CopyElevation()
    {
      var obj = EditorContext.SelectedObject! as ITransformableObject;
      if (obj == null)
        return;
      _savedElevation = obj.Position.y;
    }

    public static void PasteElevation()
    {
      var obj = EditorContext.SelectedObject! as ITransformableObject;
      if (obj == null)
        return;
      EditorContext.ChangeManager.AddChange(new TransformObject(obj).Move(obj.Position.x, _savedElevation, obj.Position.z));
    }

    #endregion
  }
}
