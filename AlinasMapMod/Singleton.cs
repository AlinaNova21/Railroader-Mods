
using System;

namespace AlinasMapMod;

public class Singleton<T> where T : class
{
  private static T _instance;
  public static T Instance
  {
    get {
      if (_instance == null) {
        _instance = Activator.CreateInstance(typeof(T), true) as T;
      }
      return _instance;
    }
  }
}
