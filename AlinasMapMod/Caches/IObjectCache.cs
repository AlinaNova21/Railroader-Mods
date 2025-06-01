using System.Collections.Generic;

namespace AlinasMapMod.Caches;
public interface IObjectCache<T> : IDictionary<string, T>
{
  public void Rebuild();
}
