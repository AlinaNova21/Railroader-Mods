using System.Collections;
using System.Collections.Generic;
using UI.Builder;
using UnityEngine;

namespace MapEditor.Managers
{
  public interface IEditableObject
  {
    string Id { get; }
    bool CanEdit { get; }
    bool CanMove { get; }
    bool CanRotate { get; }
    bool CanScale { get; }
    bool CanCreate { get; }
    bool CanDestroy { get; }
    List<string> Properties { get; }
    void SetProperty<T>(string property, T value);
    T? GetProperty<T>(string property);
    void BuildUI(UIPanelBuilder builder);
    void Save();
    void Load();
  }
}
