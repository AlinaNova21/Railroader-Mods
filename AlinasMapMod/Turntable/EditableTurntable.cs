using System;
using System.Collections.Generic;
using AlinasMapMod.Definitions;
using AlinasMapMod.MapEditor;
using AlinasMapMod.Turntable;
using Newtonsoft.Json.Linq;
using StrangeCustoms.Tracks;
using UI.Builder;
using UnityEngine;

namespace MapEditor.Objects
{
  public class TurntableFactory : IObjectFactory
  {
    public string Name => "turntable";

    public bool Enabled => false; // Disabled for now
    public Type ObjectType => typeof(EditableTurntable);

    public IEditableObject CreateObject(PatchEditor editor, string id)
    {
      var go = new GameObject(id);
      var obj = go.AddComponent<EditableTurntable>();
      obj.Id = id;
      obj.Position = Vector3.zero;
      obj.Rotation = Vector3.zero;
      obj.Save(editor);
      return obj;
    }
  }
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
    public Vector3 Scale { get => Transform.localScale; set => Transform.localScale = value; }
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
      switch (property) {
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
      switch (property) {
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

    public void BuildUI(UIPanelBuilder builder, IEditorUIHelper helper)
    {
      builder.AddField("Roundhouse Stalls", builder.AddInputField(Roundhouse.Stalls.ToString() ?? "0", value => {
        if (int.TryParse(value, out var result)) {
          helper.SetProperty("RoundhouseStalls", result);
        }
      }));
      builder.AddField("Subdivisions", builder.AddInputField(Turntable.Subdivisions.ToString() ?? "0", value => {
        if (int.TryParse(value, out var result)) {
          helper.SetProperty("Subdivisions", result);
        }
      }));
      builder.AddField("Radius", builder.AddInputField(Turntable.Radius.ToString() ?? "0", value => {
        if (int.TryParse(value, out var result)) {
          helper.SetProperty("Radius", result);
        }
      }));
    }

    public void Load(PatchEditor editor)
    {
      var splineys = editor.GetSplineys();
      if (splineys == null)
        return;
      Rebuild();
    }

    public void Save(PatchEditor editor)
    {
      Rebuild();
      editor.AddOrUpdateSpliney(Turntable.Identifier, _ => GetSpliney());
    }

    public void Destroy(PatchEditor editor)
    {
      //var ttb = new TurntableBuilder();
      //ttb.DestroySpliney(Turntable.Identifier);
      editor.RemoveSpliney(Turntable.Identifier);
      Destroy(Turntable.transform.gameObject);
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
