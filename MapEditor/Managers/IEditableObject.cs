using System.Collections;
using System.Collections.Generic;
using UI.Builder;
using UnityEngine;

namespace MapEditor.Managers
{
  public interface IEditableObject
  {
    string Id { get; set; }
    string DisplayType { get; }
    bool CanEdit { get; }
    bool CanMove { get; }
    bool CanRotate { get; }
    bool CanScale { get; }
    bool CanCreate { get; }
    bool CanDestroy { get; }
    List<string> Properties { get; }
    void SetProperty(string property, object value);
    object GetProperty(string property);
    void BuildUI(UIPanelBuilder builder);
    void Save();
    void Load();
  }
}
