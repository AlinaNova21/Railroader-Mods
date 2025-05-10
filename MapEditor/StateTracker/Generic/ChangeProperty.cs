using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MapEditor.Managers;

namespace MapEditor.StateTracker.Generic
{
  public class ChangeProperty : IUndoable
  {
    private readonly IEditableObject _Object;
    private readonly ObjectGhost _Old;
    private readonly ObjectGhost _New;
    private bool _IsEditable;

    public ChangeProperty(IEditableObject obj)
    {
      _Object = obj;
      _Old = new ObjectGhost(obj.Id);
      _New = new ObjectGhost(obj.Id);
      _New.UpdateGhost(obj);
      _IsEditable = true;
    }

    public ChangeProperty SetProperty(string property, object value)
    {
      if (!_IsEditable || !_Object.Properties.Contains(property))
      {
        throw new InvalidOperationException();
      }
      _New.Properties[property] = value;
      return this;
    }
    public void Apply()
    {
      _IsEditable = false;
      _Old.UpdateGhost(_Object);
      _New.UpdateObject(_Object);
      _Object.Save();
    }

    public void Revert()
    {
      _Old.UpdateObject(_Object);
      _Object.Save();
    }

    public override string ToString()
    {
      return "ChangeProperty: " + _Object.Id;
    }
  }
}
