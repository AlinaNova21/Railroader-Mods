using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AlinasMapMod.Definitions;
using AlinasMapMod.MapEditor;
using AlinasMapMod.Validation;
using Model.Ops;
using Newtonsoft.Json.Linq;
using Serilog;
using StrangeCustoms.Tracks;
using UI.Builder;
using UnityEngine;

using AlinasMapMod;

namespace AlinasMapMod.Stations;
partial class PaxStationAgent : IEditableObject, ITransformableObject, ICustomHelper, IValidatable
{
  public string Id { get => identifier; set => identifier = value; }

  public string DisplayType => "Station Agent";

  public bool CanEdit => true;

  public bool CanCreate => true;

  public bool CanDestroy => true;

  public List<string> Properties => ["Prefab", "PassengerStop"];

  public bool CanMove => true;

  public bool CanRotate => true;

  public bool CanScale => false;

  public Vector3 Position { get => Transform.localPosition; set => Transform.localPosition = value; }
  public Vector3 Rotation { get => Transform.localEulerAngles; set => Transform.localEulerAngles = value; }
  public Vector3 Scale { get => Transform.localScale; set => Transform.localScale = value; }

  public Transform Transform => transform;

  private GameObject _helperPrefab;
  public GameObject HelperPrefab
  {
    get {
      if (_helperPrefab == null) {
        _helperPrefab = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        _helperPrefab.transform.localScale = new Vector3(0.4f, 5f, 0.4f);
        _helperPrefab.transform.localPosition = new Vector3(0, 5f, 0);
      }
      return _helperPrefab;
    }
  }

  public void BuildUI(UIPanelBuilder builder, IEditorUIHelper helper)
  {
    var prefabs = VanillaPrefabs.AvailableStationPrefabs.ToList();
    var selectedValue = ((string)helper.GetProperty("Prefab")).Replace("vanilla://", "") ?? "";
    var selectedIndex = prefabs.IndexOf(selectedValue);
    builder.AddDropdown(prefabs, selectedIndex, (index) => {
      var prefab = prefabs[index];
      helper.SetProperty("Prefab", "vanilla://" + prefab);
    });
    var stops = new List<string>();
    var allStops = GameObject.FindObjectsOfType<PassengerStop>(true);
    foreach (var stop in allStops) {
      stops.Add(stop.identifier);
    }
    var industrySelectedIndex = stops.IndexOf((string)helper.GetProperty("PassengerStop"));
    stops.Sort();
    builder.AddDropdown(stops, industrySelectedIndex, (index) => {
      var industry = stops[index];
      helper.SetProperty("PassengerStop", industry);
    });
  }
  public void Load(PatchEditor editor)
  {
    var splineys = editor.GetSplineys();
    if (splineys.TryGetValue(Id, out var raw)) Load(raw);
  }

  public void Load(JObject raw)
  {
    var data = raw.ToObject<SerializedStationAgent>();
    data.Write(this);
  }

  public void Save(PatchEditor editor)
  {
    Log.Logger.Debug($"Saving loader {Id}");
    editor.AddOrUpdateSpliney(Id, _ => {
      var sl = new SerializedStationAgent();
      sl.Read(this);
      var obj = JObject.FromObject(sl, AlinasMapMod.JsonSerializer);
      obj.Add("handler", "AlinasMapMod.Stations.StationAgentBuilder");
      return obj;
    });
  }

  public object GetProperty(string property)
  {
    return property switch
    {
      "Prefab" => Prefab,
      "PassengerStop" => PassengerStop,
      _ => throw new InvalidOperationException($"property not valid: {property}"),
    };
  }
  public void SetProperty(string property, object value)
  {
    ValidatePropertyChange(property, value, GetSerializedStationAgent);
    
    switch (property) {
      case "Prefab":
        Prefab = (string)value;
        break;
      case "PassengerStop":
        PassengerStop = (string)value;
        break;
      default:
        throw new InvalidOperationException($"property not valid: {property}");
    }
  }

  private SerializedStationAgent GetSerializedStationAgent()
  {
    var serializedStationAgent = new SerializedStationAgent();
    serializedStationAgent.Read(this);
    return serializedStationAgent;
  }
  
  public void Validate()
  {
    GetSerializedStationAgent().Validate();
  }
  
  public ValidationResult ValidateWithDetails()
  {
    return GetSerializedStationAgent().ValidateWithDetails();
  }
  
  private void ValidatePropertyChange<T>(string property, object value, Func<T> getValidationObject) where T : IValidatable
  {
    var validationObject = getValidationObject();
    
    // Apply the proposed change using reflection
    var propertyInfo = typeof(T).GetProperty(property);
    if (propertyInfo == null)
      throw new InvalidOperationException($"Property {property} not found on {typeof(T).Name}");
      
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
  
  public void Destroy(PatchEditor editor)
  {
    editor.RemoveSpliney(Id);
    var sl = new SerializedStationAgent();
    sl.Destroy(this);
  }
}
