using Game.Progression;

namespace AlinasMapMod.Caches;

public class SectionCache : ComponentCache<SectionCache, Section>
{
  public override string GetIdentifier(Section obj) => obj.identifier;
}
