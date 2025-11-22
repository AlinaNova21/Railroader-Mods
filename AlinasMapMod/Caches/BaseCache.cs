using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GalaSoft.MvvmLight.Messaging;
using Serilog;

namespace AlinasMapMod.Caches;

public abstract class BaseCache<IType, CType> : Dictionary<string, CType>, IObjectCache<CType>
{
  protected readonly ILogger logger = Log.ForContext(typeof(IType));
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
