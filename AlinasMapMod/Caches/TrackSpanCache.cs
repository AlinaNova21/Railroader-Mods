using Track;

namespace AlinasMapMod.Caches;

public class TrackSpanCache : ComponentCache<TrackSpanCache, TrackSpan>
{
  public override string GetIdentifier(TrackSpan obj) => obj.id;
}
