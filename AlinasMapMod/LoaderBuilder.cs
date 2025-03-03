using System;
using System.Data.Odbc;
using System.Linq;
using System.Reflection;
using AlinasMapMod.Definitions;
using Helpers.Culling;
using Microsoft.SqlServer.Server;
using Model.Ops;
using Newtonsoft.Json.Linq;
using RollingStock;
using RollingStock.Controls;
using Serilog;
using Track;
using UnityEngine;
using UnityEngine.UI;

namespace AlinasMapMod;

public class LoaderBuilder : StrangeCustoms.ISplineyBuilder
{
  Serilog.ILogger logger = Log.ForContext<LoaderBuilder>();
  public GameObject BuildSpliney(string id, Transform parentTransform, JObject data)
  {
    var loader = data.ToObject<SerializedLoader>();
    logger.Information($"Configuring loader {id} with prefab {loader.Prefab}");

    var pos = new Vector3(loader.Position.x, loader.Position.y, loader.Position.z);
    var rot = new Vector3(loader.Rotation.x, loader.Rotation.y, loader.Rotation.z);

    var prefab = Utils.GameObjectFromUri(loader.Prefab);

    if (prefab == null)
    {
      Log.Error("Loader prefab not found: {prefab}", loader.Prefab);
      throw new ArgumentException("Loader prefab not found " + loader.Prefab);
    }

    var parent = GameObject.Find("Large Scenery");
    var go = new GameObject(id);
    go.transform.parent = parent.transform;
    var lgo = GameObject.Instantiate(prefab, go.transform);
    go.name = id;
    go.SetActive(false);
    go.transform.localPosition = pos;
    go.transform.localEulerAngles = rot;

    var trackMarker = lgo.GetComponent<TrackMarker>();
    if(trackMarker != null)
    {
      trackMarker.enabled = false;
    }

    var gkvo = lgo.GetComponent<GlobalKeyValueObject>();
    gkvo.globalObjectId = id + ".loader";

    foreach(Renderer r in lgo.transform.GetComponentsInChildren<Renderer>()) {
      r.enabled = true; // enable all renderers
    }

    if (loader.Industry != "")
    {
      var industry = GameObject.FindObjectsByType<Industry>(FindObjectsSortMode.None).Single(v => v.identifier == loader.Industry);
      var targetLoader = lgo.GetComponentInChildren<CarLoadTargetLoader>();
      targetLoader.sourceIndustry = industry;
    }

    go.SetActive(true);
    lgo.SetActive(true);
    return go;
  }
}
