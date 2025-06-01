using System.Collections.Generic;
using GalaSoft.MvvmLight.Messaging;

namespace AlinasMapMod.Caches;

public abstract class BaseCache<IType, CType> : Dictionary<string, CType>, IObjectCache<CType>
{
  private static IType instance;
  public static IType Instance
  {
    get {
      instance ??= (IType)System.Activator.CreateInstance(typeof(IType), true);
      return instance;
    }
  }
  protected BaseCache()
  {
    Messenger.Default.Register<CachesNeedRebuildEvent>(this, _ => Rebuild());
    Rebuild();
  }
  public abstract void Rebuild();

  public static void RebuildAll() => Messenger.Default.Send(new CachesNeedRebuildEvent());
}
