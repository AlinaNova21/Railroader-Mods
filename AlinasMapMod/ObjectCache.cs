using System;
using AlinasMapMod.Caches;

namespace AlinasMapMod;

[Obsolete("Use cache instances instead of this class directly. This class will be removed in a future version.")]
public class ObjectCache : Singleton<ObjectCache>
{

  #region Vanilla Objects
  public MapFeatureCache MapFeatures => MapFeatureCache.Instance;
  public SectionCache Sections => SectionCache.Instance;
  public IndustryCache Industries => IndustryCache.Instance;
  public IndustryComponentCache IndustryComponents => IndustryComponentCache.Instance;
  public TrackSpanCache TrackSpans => TrackSpanCache.Instance;
  public LoadCache Loads => LoadCache.Instance;
  public AreaCache Areas => AreaCache.Instance;
  public TrackNodeCache TrackNodes => TrackNodeCache.Instance;
  public TrackSegmentCache TrackSegments => TrackSegmentCache.Instance;
  public MapLabelCache MapLabels => MapLabelCache.Instance;
  #endregion

  #region Custom Objects
  public LoaderCache LoaderInstances => LoaderCache.Instance;
  public StationAgentCache PaxStationAgents => StationAgentCache.Instance;
  #endregion

  public void Rebuild()
  {
    MapFeatures.Rebuild();
    Sections.Rebuild();
    Industries.Rebuild();
    IndustryComponents.Rebuild();
    TrackSpans.Rebuild();
    Loads.Rebuild();
    Areas.Rebuild();
    TrackNodes.Rebuild();
    TrackSegments.Rebuild();
    MapLabels.Rebuild();
    LoaderInstances.Rebuild();
    PaxStationAgents.Rebuild();
  }
}
