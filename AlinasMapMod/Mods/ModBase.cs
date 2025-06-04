using Serilog;
using UnityEngine;

namespace AlinasMapMod.Mods;
public abstract class ModBase : MonoBehaviour, ILoadableMod
{
  protected readonly Serilog.ILogger Logger;

  public ModBase() => Logger = Log.ForContext(GetType());

  static internal T CreateInstance<T>() where T : ModBase
  {
    var go = new GameObject(typeof(T).Name).AddComponent(typeof(T));
    DontDestroyOnLoad(go.gameObject);
    return (T)go;
  }

  public virtual void Load()
  {
    Logger.Debug($"Load");
  }
  public virtual void Unload()
  {
    Logger.Debug($"Unload");
  }
}
