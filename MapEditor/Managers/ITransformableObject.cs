using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MapEditor.Managers
{
  public interface ITransformableObject: IEditableObject
  {
    Vector3 Position { get; set; }
    Vector3 Rotation { get; set; }

    Transform Transform { get; }
  }
}
