using System.CodeDom;
using System.Collections.Generic;
using AlinasMapMod.Turntable;
using MapEditor.Managers;
using Newtonsoft.Json.Linq;
using UI.Builder;
using UnityEngine;

namespace MapEditor.Objects
{
  public class EditableTurntable(TurntableComponent turntable) : IEditableObject, ITransformableObject
  {
    public string Id { get; set; }
    public bool CanEdit => true;
    public bool CanMove => true;
    public bool CanRotate => true;
    public bool CanScale => false;
    public bool CanCreate => false;
    public bool CanDestroy => false;
    public bool IsSpliney => true;

    public Vector3 Position { get => Transform.localPosition; set => Transform.localPosition = value; }
    public Vector3 Rotation { get => Transform.localEulerAngles; set => Transform.localEulerAngles = value; }
    public Transform Transform => turntable.transform;

    private JObject Props = new JObject();

    public List<string> Properties => new List<string>
    {
      "Subdivisions",
      "RoundhouseStalls",
      "Radius",
      "StartPrefab",
      "EndPrefab",
      "StallPrefab",
    };

    public T? GetProperty<T>(string property)
    {
      var token = Props.GetValue(property, System.StringComparison.OrdinalIgnoreCase);
      if (token == null)
        return default;
      return token.ToObject<T?>();
    }
    public void SetProperty<T>(string property, T value)
    {
      if (value == null)
        Props.Remove(property);
      else
        Props[property] = JToken.FromObject(value);
    }

    public void BuildUI(UIPanelBuilder builder)
    {

    }

    public void Load()
    {
      var splineys = EditorContext.PatchEditor!.GetSplineys();
      if (splineys == null)
        return;
      var ttb = new TurntableBuilder();
      ttb.BuildSpliney(turntable.Identifier, Transform, splineys[Id]);
      Props = splineys[Id];
    }

    public void Save()
    {
      var ttb = new TurntableBuilder();
      ttb.BuildSpliney(turntable.Identifier, Transform, Props);
      EditorContext.PatchEditor!.AddOrUpdateSpliney(turntable.Identifier, _ => Props);
    }

  }
}
