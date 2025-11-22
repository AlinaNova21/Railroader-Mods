using System;
using AlinasMapMod.Definitions;
using AlinasMapMod.MapEditor;
using AlinasMapMod.Validation;
using Newtonsoft.Json.Linq;
using StrangeCustoms.Tracks;
using UnityEngine;

namespace AlinasMapMod.Map;

public class MapLabelBuilder : SplineyBuilderBase, IObjectFactory
{
  public string Name => "Map Label";
  public Type ObjectType => typeof(EditableMapLabel);
  public bool Enabled => true;

  protected override GameObject BuildSplineyInternal(string id, Transform parentTransform, JObject data)
  {
    Logger.Information("Building {BuilderType} with ID {Id}", GetType().Name, id);
    var result = BuildFromCreatableComponent<UI.Map.MapLabel, SerializedMapLabel>(id, data);
    Logger.Information("Successfully built {BuilderType} with ID {Id}", GetType().Name, id);
    return result;
  }

  public IEditableObject CreateObject(PatchEditor editor, string id)
  {
    var data = new SerializedMapLabel
    {
      Text = "New Map Label"
    };
    var obj = BuildSpliney(id, null, JObject.FromObject(data));
    return obj.GetComponent<EditableMapLabel>();
  }
}