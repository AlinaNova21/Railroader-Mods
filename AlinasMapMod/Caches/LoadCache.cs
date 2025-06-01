using System.Linq;
using Model;
using Model.Ops.Definition;

namespace AlinasMapMod.Caches;

public class LoadCache : BaseCache<LoadCache, Load>
{
  private LoadCache()
  {
    Rebuild();
  }
  public override void Rebuild()
  {
    Clear();
    CarPrototypeLibrary.instance.opsLoads
        .ToList()
        .ForEach(v => this[v.id] = v);
  }
}
