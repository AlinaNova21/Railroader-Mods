using Track;

namespace AlinasMapMod.Caches;

public class TrackNodeCache : ComponentCache<TrackNodeCache, TrackNode>
{
  public override string GetIdentifier(TrackNode obj) => obj.id;
}
