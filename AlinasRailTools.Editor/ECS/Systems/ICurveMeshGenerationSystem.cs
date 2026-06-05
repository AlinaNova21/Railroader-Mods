namespace AlinasRailTools.Editor.ECS.Systems;

/// <summary>
/// Interface for curve mesh generation system
/// Generates rail meshes from bezier curves
/// </summary>
public interface ICurveMeshGenerationSystem : IUpdateSystem
{
    /// <summary>
    /// Get the number of segments pending mesh generation
    /// </summary>
    int GetPendingMeshCount();
}
