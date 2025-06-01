using UnityEngine;

namespace AlinasMapMod.Definitions;

public interface IDestroyableComponent<T> where T : Component
{
  /**
   * Destroy the component.
   */
  public abstract void Destroy(T comp);
}
