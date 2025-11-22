using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AlinasMapMod.Definitions;
using AlinasMapMod.MapEditor;
using AlinasMapMod.Validation;
using Newtonsoft.Json.Linq;
using Serilog;
using StrangeCustoms.Tracks;
using TMPro;
using UI.Builder;
using UI.Map;
using UnityEngine;

using AlinasMapMod;

namespace AlinasMapMod.Map;

public class EditableMapLabel : MonoBehaviour, IEditableObject, ITransformableObject, IValidatable
{
  public string Id { get; set; }
  public string DisplayType => "Map Label";
  public bool CanEdit => true;
  public bool CanMove => true;
  public bool CanRotate => false;
  public bool CanScale => false;
  public bool CanCreate => true;
  public bool CanDestroy => true;

  public List<string> Properties => ["Text"];

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
      default:
        throw new InvalidOperationException($"property not valid: {property}");
    }
  }

  public void SetProperty(string property, object value)
  {
    ValidatePropertyChange(property, value, GetSerializedMapLabel);
    
    switch (property) {
      case "Text":
        mapLabel.text = (string)value;
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
      var clone = GameObject.Instantiate(parent.GetComponentInChildren<MapLabel>().gameObject, transform, true);
      clone.name = "MapLabel";
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

  private SerializedMapLabel GetSerializedMapLabel()
  {
    return new SerializedMapLabel
    {
      Position = Position,
      Text = mapLabel?.text ?? "Map Label"
    };
  }
  
  private JObject GetSpliney()
  {
    var obj = new JObject
    {
      ["handler"] = "AlinasMapMod.MapLabelBuilder",
      ["position"] = JObject.FromObject(Position, AlinasMapMod.JsonSerializer),
      ["text"] = mapLabel.text
    };
    return obj;
  }
  
  public void Validate()
  {
    GetSerializedMapLabel().Validate();
  }
  
  public ValidationResult ValidateWithDetails()
  {
    return GetSerializedMapLabel().ValidateWithDetails();
  }
  
  private void ValidatePropertyChange<T>(string property, object value, Func<T> getValidationObject) where T : IValidatable
  {
    var validationObject = getValidationObject();
    
    // Apply the proposed change using reflection
    var actualProperty = property;
    
    // Apply the proposed change using reflection
    var propertyInfo = typeof(T).GetProperty(actualProperty);
    if (propertyInfo == null)
      throw new InvalidOperationException($"Property {actualProperty} not found on {typeof(T).Name}");
      
    propertyInfo.SetValue(validationObject, value);
    
    // Validate the change
    try
    {
      validationObject.Validate();
    }
    catch (ValidationException ex)
    {
      Log.ForContext(GetType()).Warning("Validation failed for {Property}: {Message}", property, ex.Message);
      throw new InvalidOperationException($"Invalid value for {property}: {ex.Message}", ex);
    }
  }
}
