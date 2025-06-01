using System;
using System.Collections.Generic;
using System.Linq;
using AlinasMapMod.Definitions;
using AlinasMapMod.MapEditor;
using Model.Ops;
using Newtonsoft.Json.Linq;
using Serilog;
using StrangeCustoms.Tracks;
using UI.Builder;
using UnityEngine;

namespace AlinasMapMod.Loaders;
partial class LoaderInstance : IEditableObject, ITransformableObject
{
  public string Id { get => identifier; set => identifier = value; }

  public string DisplayType => "Loader";

  public bool CanEdit => true;

  public bool CanCreate => true;

  public bool CanDestroy => true;

  public List<string> Properties => ["Prefab", "Industry"];

  public bool CanMove => true;

  public bool CanRotate => true;

  public bool CanScale => false;

  public Vector3 Position { get => Transform.localPosition; set => Transform.localPosition = value; }
  public Vector3 Rotation { get => Transform.localEulerAngles; set => Transform.localEulerAngles = value; }
  public Vector3 Scale { get => Transform.localScale; set => Transform.localScale = value; }

  public Transform Transform => transform;

  public void BuildUI(UIPanelBuilder builder, IEditorUIHelper helper)
  {
    var prefabs = VanillaPrefabs.AvailableLoaderPrefabs.ToList();
    var selectedValue = ((string)helper.GetProperty("Prefab")).Replace("vanilla://", "") ?? "";
    var selectedIndex = prefabs.IndexOf(selectedValue);
    builder.AddDropdown(prefabs, selectedIndex, (index) => {
      var prefab = prefabs[index];
      helper.SetProperty("Prefab", "vanilla://" + prefab);
    });
    var industries = new List<string>();
    var allIndustries = GameObject.FindObjectsOfType<Industry>();
    foreach (var industry in allIndustries) {
      industries.Add(industry.identifier);
    }
    var industrySelectedIndex = industries.IndexOf((string)helper.GetProperty("Industry"));
    industries.Sort();
    builder.AddDropdown(industries, industrySelectedIndex, (index) => {
      var industry = industries[index];
      helper.SetProperty("Industry", industry);
    });
  }
  public void Load(PatchEditor editor)
  {
    var splineys = editor.GetSplineys();
    if (splineys.TryGetValue(Id, out var raw)) Load(raw);
  }

  public void Load(JObject raw)
  {
    var data = raw.ToObject<SerializedLoader>();
    data.Write(this);
  }

  public void Save(PatchEditor editor)
  {
    Log.Logger.Debug($"Saving loader {Id}");
    editor.AddOrUpdateSpliney(Id, _ => {
      var sl = new SerializedLoader();
      sl.Read(this);
      var obj = JObject.FromObject(sl, AlinasMapMod.JsonSerializer);
      obj.Add("handler", "AlinasMapMod.Loaders.LoaderBuilder");
      return obj;
    });
  }

  public object GetProperty(string property)
  {
    return property switch
    {
      "Prefab" => Prefab,
      "Industry" => Industry,
      _ => throw new InvalidOperationException($"property not valid: {property}"),
    };
  }
  public void SetProperty(string property, object value)
  {
    switch (property) {
      case "Prefab":
        Prefab = (string)value;
        break;
      case "Industry":
        Industry = (string)value;
        break;
      default:
        throw new InvalidOperationException($"property not valid: {property}");
    };
  }

  public void Destroy(PatchEditor editor)
  {
    editor.RemoveSpliney(Id);
    var sl = new SerializedLoader();
    sl.Destroy(this);
  }
}
