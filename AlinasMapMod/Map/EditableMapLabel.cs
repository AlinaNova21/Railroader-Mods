using System;
using System.Collections.Generic;
using AlinasMapMod.Definitions;
using AlinasMapMod.MapEditor;
using Newtonsoft.Json.Linq;
using Serilog;
using StrangeCustoms.Tracks;
using TMPro;
using UI.Builder;
using UI.Map;
using UnityEngine;

namespace AlinasMapMod.Map;

public class EditableMapLabel : MonoBehaviour, IEditableObject, ITransformableObject
{
  public string Id { get; set; }
  public string DisplayType => "Map Label";
  public bool CanEdit => true;
  public bool CanMove => true;
  public bool CanRotate => false;
  public bool CanScale => false;
  public bool CanCreate => true;
  public bool CanDestroy => true;

  public List<string> Properties => ["Text", "Alignment"];

  public Vector3 Position { get => Transform.localPosition; set => Transform.localPosition = value; }
  public Vector3 Rotation
  {
    get => throw new InvalidOperationException("Rotation is not supported for map labels");
    set => throw new InvalidOperationException("Rotation is not supported for map labels");
  }
  public Vector3 Scale { get => Transform.localScale; set => Transform.localScale = value; }
  public Transform Transform => transform;

  MapLabel mapLabel => GetComponentInChildren<MapLabel>();

  public EditableMapLabel()
  {
    Rebuild();
  }

  public void BuildUI(UIPanelBuilder builder, IEditorUIHelper helper)
  {
    builder.AddField("Text", builder.AddInputField(mapLabel.text, v => helper.SetProperty("Text", v)));
  }

  public void Destroy(PatchEditor editor)
  {
    editor.RemoveSpliney(Id);
    Destroy(gameObject);
  }

  public object GetProperty(string property)
  {
    switch (property) {
      case "Text":
        return mapLabel.text;
      case "Alignment":
        return mapLabel.alignment.ToString();
    }
    return null;
  }

  public void SetProperty(string property, object value)
  {
    switch (property) {
      case "Text":
        mapLabel.text = (string)value;
        break;
      case "Alignment":
        mapLabel.alignment = (MapLabel.Alignment)Enum.Parse(typeof(MapLabel.Alignment), (string)value);
        break;
      default:
        throw new InvalidOperationException($"property not valid: {property}");
    }
    Rebuild();
  }

  public void Load(PatchEditor editor)
  {
    var splineys = editor.GetSplineys();
    if (splineys.TryGetValue(Id, out var raw)) Load(raw);
  }

  public void Load(JObject raw)
  {
    Rebuild();
    var data = raw.ToObject<SerializedMapLabel>();
    Position = data.Position;
    mapLabel.text = data.Text;
    mapLabel.alignment = data.Alingnment;
    var text = GetComponentInChildren<TextMeshProUGUI>();
    text.text = data.Text;
  }

  public void Save(PatchEditor editor)
  {
    Rebuild();
    Log.Logger.Debug($"Saving map label {Id}");
    Log.Logger.Debug($"Data: {GetSpliney().ToString()}");
    editor.AddOrUpdateSpliney(Id, _ => GetSpliney());
  }

  private void Rebuild()
  {
    var parent = GameObject.Find("Map Labels");
    transform.parent = parent.transform;

    var mapLabel = GetComponentInChildren<MapLabel>();

    if (mapLabel == null) {
      var clone = GameObject.Instantiate(parent.GetComponentInChildren<MapLabel>().gameObject);
      clone.name = "MapLabel";
      clone.transform.SetParent(transform);
      clone.transform.localPosition = Vector3.zero;
      mapLabel = clone.GetComponent<MapLabel>();
      var canvas = clone.GetComponent<Canvas>();
      typeof(MapLabel)
        .GetField("_canvas", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
        .SetValue(mapLabel, canvas);
      mapLabel.text = "New Map Label";
    }

    var text = GetComponentInChildren<TextMeshProUGUI>();
    text!.text = mapLabel.text;
  }

  private JObject GetSpliney()
  {
    var obj = new JObject
    {
      ["handler"] = "AlinasMapMod.MapLabelBuilder",
      ["position"] = JObject.FromObject(Position, AlinasMapMod.JsonSerializer),
      ["text"] = mapLabel.text,
      ["alignment"] = mapLabel.alignment.ToString()
    };
    return obj;
  }
}
