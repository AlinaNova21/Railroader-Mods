using Game.Progression;

namespace AlinasMapMod.Caches;

public class MapFeatureCache : ComponentCache<MapFeatureCache, MapFeature>
{
  public override string GetIdentifier(MapFeature obj) => obj.identifier;
}
