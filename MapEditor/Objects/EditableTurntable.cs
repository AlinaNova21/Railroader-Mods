using System;
using System.CodeDom;
using System.Collections.Generic;
using AlinasMapMod.Definitions;
using AlinasMapMod.Turntable;
using MapEditor.Managers;
using Newtonsoft.Json.Linq;
using UI.Builder;
using UnityEngine;

namespace MapEditor.Objects
{
  public class EditableTurntable : MonoBehaviour, IEditableObject, ITransformableObject
  {
    private TurntableComponent Turntable => GetComponent<TurntableComponent>() ?? gameObject.AddComponent<TurntableComponent>();
    private RoundhouseComponent Roundhouse => GetComponent<RoundhouseComponent>() ?? gameObject.AddComponent<RoundhouseComponent>();
    public string Id { get => Turntable.Identifier; set => Turntable.Identifier = value; }
    public string DisplayType => "Turntable";
    public bool CanEdit => true;
    public bool CanMove => true;
    public bool CanRotate => true;
    public bool CanScale => false;
    public bool CanCreate => false;
    public bool CanDestroy => false;
    public bool IsSpliney => true;

    public Vector3 Position { get => Transform.localPosition; set => Transform.localPosition = value; }
    public Vector3 Rotation { get => Transform.localEulerAngles; set => Transform.localEulerAngles = value; }
    public Transform Transform => transform;

    private string StartPrefab { get; set; } = "vanilla";
    private string EndPrefab { get; set; } = "vanilla";
    private string StallPrefab { get; set; } = "vanilla";

    public List<string> Properties =>
    [
      "Subdivisions",
      "RoundhouseStalls",
      "Radius",
      "StartPrefab",
      "EndPrefab",
      "StallPrefab",
    ];

    public object GetProperty(string property)
    {
      switch(property)
      {
        case "Subdivisions":
          return Turntable.Subdivisions;
        case "RoundhouseStalls":
          return Roundhouse.Stalls;
        case "Radius":
          return Turntable.Radius;
        case "StartPrefab":
          return StartPrefab;
        case "EndPrefab":
          return EndPrefab;
        case "StallPrefab":
          return StallPrefab;
        default:
          throw new InvalidOperationException($"property not valid: {property}");
      }
    }
    public void SetProperty(string property, object value)
    {
      switch (property)
      {
        case "Subdivisions":
          Turntable.Subdivisions = (int)value;
          Turntable.Build();
          break;
        case "RoundhouseStalls":
          Roundhouse.Stalls = (int)value;
          Roundhouse.Build();
          break;
        case "Radius":
          Turntable.Radius = (int)value;
          Turntable.Build();
          break;
        case "StartPrefab":
          StartPrefab = (string)value;
          Roundhouse.Build();
          break;
        case "EndPrefab":
          EndPrefab = (string)value;
          Roundhouse.Build();
          break;
        case "StallPrefab":
          StallPrefab = (string)value;
          Roundhouse.Build();
          break;
        default:
          throw new InvalidOperationException($"property not valid: {property}");
      }
     }

    public void BuildUI(UIPanelBuilder builder)
    {
      builder.AddField("Roundhouse Stalls", builder.AddInputField(Roundhouse.Stalls.ToString() ?? "0", value =>
      {
        if (int.TryParse(value, out var result))
        {
          Roundhouse.Stalls = result;
          Roundhouse.Build();
        }
      }));
      builder.AddField("Subdivisions", builder.AddInputField(Turntable.Subdivisions.ToString() ?? "0", value =>
      {
        if (int.TryParse(value, out var result))
        {
          Turntable.Subdivisions = result;
          Turntable.Build();
        }
      }));
      builder.AddField("Radius", builder.AddInputField(Turntable.Radius.ToString() ?? "0", value =>
      {
        if (int.TryParse(value, out var result))
        {
          Turntable.Radius = result;
          Turntable.Build();
        }
      }));
    }

    public void Load()
    {
      var splineys = EditorContext.PatchEditor!.GetSplineys();
      if (splineys == null)
        return;
      Rebuild();
    }

    public void Save()
    {
      Rebuild();
      EditorContext.PatchEditor!.AddOrUpdateSpliney(Turntable.Identifier, _ => GetSpliney());
    }

    private void Rebuild()
    {
      var ttb = new TurntableBuilder();
      ttb.BuildSpliney(Turntable.Identifier, Transform, GetSpliney());
    }

    private JObject GetSpliney()
    {
      var obj = new JObject();
      obj["Position"] = JObject.FromObject((SerializedVector3)Position);
      obj["Rotation"] = JObject.FromObject((SerializedVector3)Rotation);
      obj["RoundhouseStalls"] = Roundhouse.Stalls;
      obj["Radius"] = Turntable.Radius;
      obj["StartPrefab"] = StartPrefab;
      obj["EndPrefab"] = EndPrefab;
      obj["StallPrefab"] = StallPrefab;
      return obj;
    }
  }
}
