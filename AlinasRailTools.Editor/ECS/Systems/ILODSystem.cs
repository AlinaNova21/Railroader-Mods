using Arch.Core;
using AlinasRailTools.Editor.Rendering;

namespace AlinasRailTools.Editor.ECS.Systems;

/// <summary>
/// Interface for the LOD system
/// Manages Level of Detail by toggling components based on distance
/// </summary>
public interface ILODSystem : ISystem
{
    /// <summary>
    /// Update LOD levels based on camera distance
    /// </summary>
    void Update(World world, Camera camera);
}
