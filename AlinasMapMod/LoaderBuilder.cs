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
  readonly Serilog.ILogger logger = Log.ForContext<LoaderBuilder>();

  public class CustomLoader : MonoBehaviour
  {
    readonly Serilog.ILogger logger = Log.ForContext<CustomLoader>();
    public string id;
    public SerializedLoader config;

    public void Rebuild()
    {
      var loader = config;
      var go = transform.gameObject;
      Vector3 pos = loader.Position;
      Vector3 rot = loader.Rotation;

      if (loader.Prefab == "")
      {
        logger.Error($"Loader prefab not set for loader: {id}");
        return;
      }
      var prefab = Utils.GameObjectFromUri(loader.Prefab);

      if (prefab == null)
      {
        Log.Error("Loader prefab not found: {prefab}", loader.Prefab);
        throw new ArgumentException("Loader prefab not found " + loader.Prefab);
      }

      var oldPrefab = transform.Find("prefab")?.gameObject;
      if (oldPrefab != null)
      {
        GameObject.Destroy(oldPrefab);
      }

      var lgo = GameObject.Instantiate(prefab, go.transform);
      lgo.name = "prefab";
      go.name = id;

      go.SetActive(false);
      go.transform.localPosition = pos;
      go.transform.localEulerAngles = rot;

      var trackMarker = lgo.GetComponent<TrackMarker>();
      if (trackMarker != null)
      {
        trackMarker.enabled = false;
      }

      var gkvo = lgo.GetComponent<GlobalKeyValueObject>();
      gkvo.globalObjectId = id + ".loader";

      foreach (Renderer r in lgo.transform.GetComponentsInChildren<Renderer>())
      {
        r.enabled = true; // enable all renderers
      }

      if (loader.Industry != "")
      {
        try
        {
          var industry = GameObject.FindObjectsByType<Industry>(FindObjectsSortMode.None).Single(v => v.identifier == loader.Industry);
          var targetLoader = lgo.GetComponentInChildren<CarLoadTargetLoader>();
          targetLoader.sourceIndustry = industry;

          var indHover = lgo.GetComponentInChildren<IndustryContentHoverable>();
          if (indHover != null)
          {
            indHover.GetType().GetField("industry", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(indHover, industry);
          }
        }
        catch (Exception e)
        {
          logger.Error(e, "Failed to find industry {industry} for loader {loader}", loader.Industry, id);
        }
      }

      go.SetActive(true);
      lgo.SetActive(true);
    }

    public static CustomLoader FindById(string id)
    {
      var go = GameObject.Find(id);
      if (go == null)
      {
        return null;
      }
      var cl = go.GetComponent<CustomLoader>();
      if (cl == null)
      {
        return null;
      }
      return cl;
    }
  }

  public GameObject BuildSpliney(string id, Transform parentTransform, JObject data)
  {
    var loader = data.ToObject<SerializedLoader>();

    logger.Information($"Configuring loader {id} with prefab {loader.Prefab}");

    var parent = GameObject.Find("Large Scenery");
    var go = new GameObject(id);
    go.transform.parent = parent.transform;
    var cl = go.AddComponent<CustomLoader>();
    cl.id = id;
    cl.config = loader;
    cl.Rebuild();
    return go;
  }
}
