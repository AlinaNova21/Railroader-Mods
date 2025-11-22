using System;
using System.Linq;
using System.Reflection;
using Model.Ops;
using RollingStock;
using RollingStock.Controls;
using Serilog;
using Track;
using UnityEngine;

namespace AlinasMapMod.Loaders;
public partial class LoaderInstance : MonoBehaviour
{
  readonly Serilog.ILogger logger = Log.ForContext<LoaderInstance>();

  public string identifier;
  private string _prefab = "";
  public string Prefab
  {
    get => _prefab;
    set {
      _prefab = value;
      RebuildPrefab();
    }
  }

  private string _industry = "";
  public string Industry
  {
    get => _industry;
    set {
      _industry = value;
      RebuildIndustry();
    }
  }

  private void RebuildPrefab()
  {
    if (Prefab == null) {
      logger.Error($"Loader prefab not set for loader: {identifier}");
      return;
    }
    var prefab = Utils.GameObjectFromUri(Prefab);
    if (prefab == null) {
      logger.Error($"Loader prefab not found: {prefab}");
      throw new ArgumentException("Loader prefab not found " + prefab);
    }

    var oldPrefab = transform.Find("prefab")?.gameObject;
    if (oldPrefab != null) Destroy(oldPrefab);

    var lgo = Instantiate(prefab, transform);
    lgo.SetActive(false);
    lgo.name = "prefab";
    transform.name = identifier;

    var trackMarker = lgo.GetComponent<TrackMarker>();
    if (trackMarker != null) trackMarker.enabled = false;

    var gkvo = lgo.GetComponent<GlobalKeyValueObject>();
    gkvo.globalObjectId = identifier + ".loader";
    foreach (Renderer r in lgo.transform.GetComponentsInChildren<Renderer>()) {
      r.enabled = true; // enable all renderers
    }
    RebuildIndustry();
    lgo.SetActive(true);
  }

  private void RebuildIndustry()
  {
    if (Industry != "") {
      try {
        var industry = FindObjectsByType<Industry>(FindObjectsSortMode.None).Single(v => v.identifier == Industry);
        var targetLoader = GetComponentInChildren<CarLoadTargetLoader>();
        if (targetLoader != null) targetLoader.sourceIndustry = industry;
        var indHover = GetComponentInChildren<IndustryContentHoverable>();
        if (indHover != null) indHover.GetType().GetField("industry", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(indHover, industry);
      } catch (Exception e) {
        logger.Error(e, "Failed to find industry {industry} for loader {loader}", Industry, identifier);
      }
    }
  }
  public void Rebuild() => RebuildPrefab();
  public static LoaderInstance FindById(string identifier) => GameObject
    .FindObjectsByType<LoaderInstance>(FindObjectsInactive.Include, FindObjectsSortMode.None)
    .FirstOrDefault(l => l.identifier == identifier);
}
