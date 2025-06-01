using System;
using AlinasMapMod.Definitions;
using AlinasMapMod.MapEditor;
using Helpers;
using Newtonsoft.Json.Linq;
using Serilog;
using StrangeCustoms.Tracks;
using UnityEngine;

namespace AlinasMapMod.Map;
public class MapLabelBuilder : StrangeCustoms.ISplineyBuilder, IObjectFactory
{
  readonly Serilog.ILogger logger = Log.ForContext<MapLabelBuilder>();

  public string Name => "Map Label";

  public Type ObjectType => typeof(EditableMapLabel);

  public bool Enabled => true;

  public GameObject BuildSpliney(string id, Transform parentTransform, JObject raw)
  {
    logger.Information($"Configuring map label {id}");

    var parent = GameObject.Find("Map Labels");
    var go = parent.transform.Find(id)?.gameObject ?? new GameObject(id);
    go.transform.parent = parent.transform;
    go.layer = Layers.Map;
    go.SetActive(false);
    var eml = go.GetComponent<EditableMapLabel>() ?? go.AddComponent<EditableMapLabel>();
    eml.Id = id;
    eml.Load(raw);
    go.SetActive(true);
    return new GameObject();
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
