using AlinasMapMod.Stations;

namespace AlinasMapMod.Caches;

public class StationAgentCache : ComponentCache<StationAgentCache, PaxStationAgent>
{
  public override string GetIdentifier(PaxStationAgent obj) => obj.identifier;
}
