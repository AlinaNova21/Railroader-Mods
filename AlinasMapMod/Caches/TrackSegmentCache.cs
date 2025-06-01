using Track;

namespace AlinasMapMod.Caches;

public class TrackSegmentCache : ComponentCache<TrackSegmentCache, TrackSegment>
{
  public override string GetIdentifier(TrackSegment obj) => obj.id;
}
