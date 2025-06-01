using UnityEngine;

namespace AlinasMapMod.Mods;

public abstract class SingletonModBase<T> : ModBase where T : SingletonModBase<T>
{
  #region Singleton
  private static T _instance;
  public static T Instance
  {
    get {
      if (_instance == null) {
        _instance = GameObject.FindFirstObjectByType<T>(FindObjectsInactive.Include) ?? CreateInstance<T>();
      }
      return _instance;
    }
  }
  #endregion
}
