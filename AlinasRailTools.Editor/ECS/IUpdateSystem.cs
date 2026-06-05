using Arch.Core;

namespace AlinasRailTools.Editor.ECS;

/// <summary>
/// Interface for systems that participate in the Update phase
/// Systems implementing this interface will be called during the main update loop
/// </summary>
public interface IUpdateSystem : ISystem
{
    /// <summary>
    /// Update the system state
    /// Called once per frame during the update phase
    /// </summary>
    void Update(World world);
}
