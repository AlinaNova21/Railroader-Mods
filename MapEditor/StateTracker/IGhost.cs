using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MapEditor.StateTracker
{
  public interface IGhost
  {
    string GetId();
    void SetId(string id);
    void SetProperty<T>(string property, T value);
    T GetProperty<T>(string property);
  }
}
