using UnityEngine;

namespace AlinasMapMod.Definitions;

/**
 * Interface for a component that can be serialized and deserialized.
 * 
 * @typeparam T The type of the component that will be serialized.
 */
public interface ISerializedPatchableComponent<T> where T : Component
{
  /**
   * Write the serialized data to the component.
   */
  public abstract void Write(T comp);
  /**
   * Read the serialized data from the component.
   */
  public abstract void Read(T comp);
  /**
   * Validate the serialized data.
   * 
   * @throws ValidationException if the data is invalid.
   */
  public abstract void Validate();
}
