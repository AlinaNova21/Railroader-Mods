using System.Collections.Generic;
using System.Linq;
using Game.Progression;
using Model;
using Model.Ops.Definition;
using Model.Ops;
using Track;
using UnityEngine;

namespace AlinasMapMod
{
  public class ObjectCache
  {
    public Dictionary<string, MapFeature> MapFeatures { get; private set; } = new Dictionary<string, MapFeature>();
    public Dictionary<string, Section> Sections { get; private set; } = new Dictionary<string, Section>();
    public Dictionary<string, Industry> Industries { get; private set; } = new Dictionary<string, Industry>();
    public Dictionary<string, IndustryComponent> IndustryComponents { get; private set; } = new Dictionary<string, IndustryComponent>();
    public Dictionary<string, TrackSpan> TrackSpans { get; private set; } = new Dictionary<string, TrackSpan>();
    public Dictionary<string, Load> Loads { get; private set; } = new Dictionary<string, Load>();
    public Dictionary<string, Area> Areas { get; private set; } = new Dictionary<string, Area>();

    public void Rebuild()
    {
      MapFeatures = Object.FindObjectsByType<MapFeature>(FindObjectsSortMode.None)
          .ToDictionary(v => v.identifier);
      Sections = Object.FindObjectsByType<Section>(FindObjectsSortMode.None)
          .ToDictionary(v => v.identifier);
      Industries = Object.FindObjectsByType<Industry>(FindObjectsSortMode.None)
          .ToDictionary(v => v.identifier);
      IndustryComponents = Object.FindObjectsByType<IndustryComponent>(FindObjectsSortMode.None)
          .ToDictionary(v => v.gameObject.GetComponentInParent<Industry>(true).identifier + "." + v.subIdentifier);
      TrackSpans = Object.FindObjectsByType<TrackSpan>(FindObjectsSortMode.None)
          .ToDictionary(v => v.id);
      Loads = CarPrototypeLibrary.instance.opsLoads
          .ToDictionary(v => v.id);
      Areas = Object.FindObjectsByType<Area>(FindObjectsSortMode.None)
          .ToDictionary(v => v.identifier);
    }
  }
}
