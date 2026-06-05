using System.Numerics;
using Arch.Core;
using Veldrid;
using AlinasRailTools.Editor.Rendering;

namespace AlinasRailTools.Editor.ECS.Systems;

/// <summary>
/// Interface for camera management system
/// Handles camera updates, input processing, and orbit controls
/// </summary>
public interface ICameraSystem : ISystem
{
    /// <summary>
    /// Update camera based on input and orbit controls
    /// </summary>
    /// <param name="world">The ECS world</param>
    /// <param name="input">Input snapshot for this frame</param>
    /// <param name="deltaTime">Time since last frame</param>
    void Update(World world, InputSnapshot input, float deltaTime);

    /// <summary>
    /// Get the active camera for rendering
    /// </summary>
    /// <param name="world">The ECS world</param>
    /// <returns>Active camera or null if none found</returns>
    Camera? GetActiveCamera(World world);

    /// <summary>
    /// Get the active camera's target position
    /// </summary>
    /// <param name="world">The ECS world</param>
    /// <returns>Camera target position or null if no active camera</returns>
    Vector3? GetCameraTarget(World world);
}
