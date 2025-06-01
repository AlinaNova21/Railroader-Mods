using UnityEngine;

namespace AlinasMapMod;

public class SingletonComponent<T> where T : MonoBehaviour
{
  private static T _instance;
  public static T Instance
  {
    get {
      if (_instance == null) {
        _instance = GameObject.FindFirstObjectByType<T>();
      }
      return _instance;
    }
  }
}
