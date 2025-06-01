using Model.Ops;

namespace AlinasMapMod.Caches;

public class PassengerStopCache : ComponentCache<PassengerStopCache, PassengerStop>
{
  public override string GetIdentifier(PassengerStop obj) => obj.identifier;
}
