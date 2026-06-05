using Arch.Core;
using Veldrid;
using AlinasRailTools.Editor.Rendering;

namespace AlinasRailTools.Editor.ECS;

/// <summary>
/// Interface for systems that participate in the Render phase
/// Systems implementing this interface will be called during the rendering loop
/// </summary>
public interface IRenderSystem : ISystem
{
    /// <summary>
    /// Render entities from the world
    /// Called once per frame during the render phase
    /// </summary>
    /// <param name="world">The ECS world</param>
    /// <param name="commandList">Command list for rendering commands</param>
    /// <param name="camera">Active camera for this frame</param>
    void Render(World world, CommandList commandList, Camera camera);
}
