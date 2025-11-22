namespace AlinasMapMod.Definitions;

/// <summary>
/// Interface for serialized components that provides standardized read/write functionality
/// for classes that don't inherit from Unity Component but need serialization operations.
/// Creation and destruction are handled by the parent component.
/// </summary>
public interface ISerializedPatchable<T>
{
    /// <summary>
    /// Writes data from this serialized object to the component
    /// </summary>
    /// <param name="obj">The component to write to</param>
    void Write(T obj);
    
    /// <summary>
    /// Reads data from the component into this serialized object
    /// </summary>
    /// <param name="obj">The component to read from</param>
    void Read(T obj);
}