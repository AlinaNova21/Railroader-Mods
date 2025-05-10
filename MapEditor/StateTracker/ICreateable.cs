using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapEditor.StateTracker
{
  public interface ICreateable
  {
    void Create();
    void Destroy();
    string GetId();
  }
}
