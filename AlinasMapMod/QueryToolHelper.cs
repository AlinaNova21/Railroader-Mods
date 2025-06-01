
using System;
using AlinasMapMod.Mods;
using GalaSoft.MvvmLight.Messaging;
using HarmonyLib;
using Helpers;
using Map.Runtime;
using Track;
using UnityEngine;

namespace AlinasMapMod;

public struct QueryTooltipUpdateEvent
{
  private Action<string> _appendText;
  public Ray Ray { get; }
  public readonly GameObject GameObject => RaycastHit.collider?.gameObject;
  public RaycastHit RaycastHit { get; }
  public bool Hit { get; }
  public readonly bool HitTerrain => GameObject?.layer == Layers.Terrain;
  public readonly bool HitTrack => GameObject?.layer == Layers.Track;

  public QueryTooltipUpdateEvent(Ray ray, Action<string> setTitle, Action<string> appendText)
  {
    _appendText = appendText;
    Ray = ray;
    if (Physics.Raycast(ray, out var raycastHit, 1000f, (1 << Layers.Terrain) | (1 << Layers.Track))) {
      Hit = true;
      RaycastHit = raycastHit;
    }
    if (HitTrack) {
      setTitle("Track");
    }
    if (HitTerrain) {
      setTitle("World");
    }
  }

  public void AppendText(string format, params object[] args)
  {
    AppendText(string.Format(format, args));
  }

  public void AppendText(string text)
  {
    _appendText(text);
  }
}

class QueryTooltipHelper : SingletonModBase<QueryTooltipHelper>
{
  public override void Load()
  {
    Messenger.Default.Register<QueryTooltipUpdateEvent>(this, Hook);
    Logger.Information("QueryTooltipHelper loaded");
  }

  public override void Unload()
  {
    Logger.Information("QueryTooltipHelper unloaded");
    Messenger.Default.Unregister(this);
  }

  private void Hook(QueryTooltipUpdateEvent @event)
  {
    if (@event.HitTrack) {
      Graph graph = TrainController.Shared.graph;
      var point = @event.RaycastHit.point;
      if (graph.TryGetLocationFromWorldPoint(point, 2f, out var location)) {
        float grade = Mathf.Abs(graph.GradeAtLocation(location));
        float curvature = graph.CurvatureAtLocation(location, Graph.CurveQueryResolution.Interpolate);

        @event.AppendText(string.Format("Grade: {0:F1}%", grade));
        @event.AppendText(string.Format("Curvature: {0:F1} deg", curvature));
      }
    }
    if (@event.Hit) {
      var raycastHit = @event.RaycastHit;
      Vector3 vector = WorldTransformer.WorldToGame(raycastHit.point);
      var mapManager = MapManager.Instance;
      var pos = mapManager.TilePositionFromPoint(vector);
      @event.AppendText("Pos: {0:F0} {1:F0} {2:F0}", vector.x, vector.y, vector.z);
      @event.AppendText("Tile: {0:F0},{1:F0}", pos.x, pos.y);
    }
  }
}

[HarmonyPatch(typeof(ObjectPicker), "QueryTooltipInfo")]
[HarmonyPatchCategory("AlinasMapMod")]
internal static class QueryTooltipInfoPrefix
{
  /**
   * The Utilities mod prevents other prefixes from running, so we need to run this prefix before it.
   * Since it doesn't run in an extensible way, we also prevent it from running.
   * All functionality of it is replicated to ensure that no functionality is lost.
  **/
  [HarmonyBefore(["Utilities"])] // Ensure this runs before the Utilities mod 
  public static bool Prefix(Ray ray, ref TooltipInfo __result)
  {
    var tooltipInfo = __result;
    tooltipInfo.Title ??= "";
    tooltipInfo.Text ??= "";
    var setTitle = new Action<string>(title => tooltipInfo.Title = title);
    var appendText = new Action<string>(text => tooltipInfo.Text += text + "\n");
    var message = new QueryTooltipUpdateEvent(ray, setTitle, appendText);
    Messenger.Default.Send(message);
    tooltipInfo.Text = tooltipInfo.Text.Trim();
    __result = tooltipInfo;
    return false;
  }
}
