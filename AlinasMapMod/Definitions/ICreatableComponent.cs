using UnityEngine;

namespace AlinasMapMod.Definitions;

public interface ICreatableComponent<T> where T : Component
{
  /**
   * Create the component.
   */
  public abstract T Create(string id);
}
