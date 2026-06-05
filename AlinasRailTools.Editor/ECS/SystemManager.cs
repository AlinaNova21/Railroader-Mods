using System.Diagnostics;
using Arch.Core;
using Veldrid;
using AlinasRailTools.Editor.ECS.Systems;

namespace AlinasRailTools.Editor.ECS;

/// <summary>
/// Centralized manager for all ECS system lifecycles
/// Systems are provided by the SystemContainer after dependency resolution
/// Handles update ordering, rendering, and disposal
/// </summary>
public class SystemManager : IDisposable
{
    private readonly World _world;
    private readonly List<ISystem> _allSystems;

    // Cached typed system lists for performance
    private readonly List<IUpdateSystem> _updateSystems;
    private readonly List<IRenderSystem> _renderSystems;

    // Specific system references for external access
    private readonly ICameraSystem _cameraSystem;
    private readonly ITerrainTileSystem? _terrainTileSystem;
    private readonly ICurveMeshGenerationSystem? _curveMeshGenerationSystem;
    private readonly ILODSystem? _lodSystem;

    // Performance tracking
    private readonly Dictionary<string, double> _systemTimings = new();
    private readonly Stopwatch _systemStopwatch = new();

    /// <summary>
    /// Create SystemManager with systems provided by container
    /// Systems are already instantiated in correct dependency order
    /// </summary>
    /// <param name="world">The ECS world</param>
    /// <param name="systems">Systems instantiated by container in dependency order</param>
    public SystemManager(World world, List<ISystem> systems)
    {
        _world = world;
        _allSystems = systems;

        // Cache typed system lists for performance
        _updateSystems = systems.OfType<IUpdateSystem>().ToList();
        _renderSystems = systems.OfType<IRenderSystem>().ToList();

        // Get specific system references
        _cameraSystem = systems.OfType<ICameraSystem>().FirstOrDefault()
            ?? throw new InvalidOperationException("ICameraSystem is required");

        _terrainTileSystem = systems.OfType<ITerrainTileSystem>().FirstOrDefault();
        _curveMeshGenerationSystem = systems.OfType<ICurveMeshGenerationSystem>().FirstOrDefault();
        _lodSystem = systems.OfType<ILODSystem>().FirstOrDefault();

        Console.WriteLine($"[SystemManager] Initialized with {_allSystems.Count} systems:");
        Console.WriteLine($"  - {_updateSystems.Count} update systems");
        Console.WriteLine($"  - {_renderSystems.Count} render systems");
        Console.WriteLine($"  - LOD System: {(_lodSystem != null ? "enabled" : "disabled")}");
    }

    /// <summary>
    /// Update camera based on input
    /// </summary>
    /// <param name="input">Input snapshot for this frame</param>
    /// <param name="deltaTime">Time since last frame</param>
    public void UpdateCamera(InputSnapshot input, float deltaTime)
    {
        _cameraSystem.Update(_world, input, deltaTime);

        // Update terrain tiles based on camera position
        if (_terrainTileSystem != null)
        {
            var cameraTarget = _cameraSystem.GetCameraTarget(_world);
            if (cameraTarget.HasValue)
            {
                _terrainTileSystem.Update(cameraTarget.Value);
            }
        }
    }

    /// <summary>
    /// Update all IUpdateSystem systems in order
    /// Systems are already in correct dependency order from container
    /// </summary>
    public void Update()
    {
        // Update all systems that implement IUpdateSystem
        foreach (var system in _updateSystems)
        {
            string systemName = system.GetType().Name;
            _systemStopwatch.Restart();
            system.Update(_world);
            _systemStopwatch.Stop();
            _systemTimings[$"Update.{systemName}"] = _systemStopwatch.Elapsed.TotalMilliseconds;
        }

        // Process pending terrain tile loads (must be on main/render thread)
        if (_terrainTileSystem != null)
        {
            _systemStopwatch.Restart();
            _terrainTileSystem.ProcessPendingLoads();
            _systemStopwatch.Stop();
            _systemTimings["Update.TerrainTileSystem.ProcessPendingLoads"] = _systemStopwatch.Elapsed.TotalMilliseconds;
        }
    }

    /// <summary>
    /// Render all IRenderSystem systems
    /// Gets active camera and passes to all render systems
    /// </summary>
    /// <param name="commandList">Command list for rendering commands</param>
    public void Render(CommandList commandList)
    {
        // Get active camera from ECS
        var camera = _cameraSystem.GetActiveCamera(_world);
        if (camera == null)
        {
            Console.WriteLine("[SystemManager] Warning: No active camera found in ECS");
            return;
        }

        // Update LOD levels based on camera distance (before rendering)
        if (_lodSystem != null)
        {
            _systemStopwatch.Restart();
            _lodSystem.Update(_world, camera);
            _systemStopwatch.Stop();
            _systemTimings["Render.LODSystem.Update"] = _systemStopwatch.Elapsed.TotalMilliseconds;
        }

        // Render all systems that implement IRenderSystem
        foreach (var system in _renderSystems)
        {
            string systemName = system.GetType().Name;
            _systemStopwatch.Restart();
            system.Render(_world, commandList, camera);
            _systemStopwatch.Stop();
            _systemTimings[$"Render.{systemName}"] = _systemStopwatch.Elapsed.TotalMilliseconds;
        }
    }

    /// <summary>
    /// Dispose all systems in reverse order (LIFO)
    /// </summary>
    public void Dispose()
    {
        // Dispose in reverse order
        for (int i = _allSystems.Count - 1; i >= 0; i--)
        {
            _allSystems[i]?.Dispose();
        }
        _allSystems.Clear();

        Console.WriteLine("[SystemManager] Disposed");
    }

    /// <summary>
    /// Get the camera system (for external queries)
    /// </summary>
    public ICameraSystem CameraSystem => _cameraSystem;

    /// <summary>
    /// Get the curve mesh generation system (for external queries)
    /// </summary>
    public ICurveMeshGenerationSystem? CurveMeshGenerationSystem => _curveMeshGenerationSystem;

    /// <summary>
    /// Get per-system timing data (for performance monitoring)
    /// </summary>
    public IReadOnlyDictionary<string, double> GetSystemTimings() => _systemTimings;
}
