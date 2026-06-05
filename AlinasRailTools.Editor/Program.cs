using System.Numerics;
using System.Diagnostics;
using Arch.Core;
using Veldrid;
using Veldrid.StartupUtilities;
using Veldrid.SPIRV;
using ImGuiNET;
using Veldrid.Sdl2;
using Serilog;
using AlinasRailTools.Editor.Rendering;
using AlinasRailTools.Editor.ECS;
using AlinasRailTools.Editor.ECS.Components;
using AlinasRailTools.Editor.ECS.DI;
using AlinasRailTools.Editor.ECS.Systems;
using AlinasRailTools.Editor.Data;
using AlinasRailTools.Editor.Logging;
using AlinasRailTools.Editor.UI;
using AlinasRailTools.Shared.Data;

namespace AlinasRailTools.Editor;

class Program
{
    private static GraphicsDevice? _graphicsDevice;
    private static CommandList? _commandList;
    private static VeldridTerrainManager? _terrainManager;
    private static GameGraphManager? _graphManager;
    private static Veldrid.Sdl2.Sdl2Window? _window;
    private static ImGuiController? _imguiController;
    private static AntialiasingManager? _aaManager;

    // ECS
    private static World? _world;
    private static SystemManager? _systemManager;
    private static MeshManager? _meshManager;

    // Database and async loading
    private static WorkingStateDB? _database;
    private static AsyncGraphLoader? _asyncLoader;

    // Logging and UI
    private static InGameConsoleSink? _consoleSink;
    private static DebugConsoleWindow? _debugConsole;
    private static SystemDebugWindow? _systemDebugWindow;
    private static PerformanceStatsWindow? _performanceStatsWindow;

    // Timing
    private static Stopwatch _stopwatch = Stopwatch.StartNew();
    private static double _lastFrameTime = 0;

    // Frame time monitoring
    private static Stopwatch _frameStopwatch = new Stopwatch();
    private static double _slowFrameThresholdMs = 33.0; // 30 FPS threshold (warn if slower than this)
    private static double _currentFrameTimeMs = 0.0;
    private static double _smoothedFPS = 60.0; // Running average FPS

    // UI state
    private static bool _showModErrorPopup = false;
    private static bool _hasShownModErrorPopup = false;

    static void Main(string[] args)
    {
        // Create custom in-game console sink
        _consoleSink = new InGameConsoleSink();

        // Initialize Serilog with multiple sinks
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File("logs/editor-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
            .WriteTo.Sink(_consoleSink)
            .CreateLogger();

        Log.Information("Alina's Rail Tools Editor starting...");

        // Create debug UI windows
        _debugConsole = new DebugConsoleWindow(_consoleSink);
        _systemDebugWindow = new SystemDebugWindow(_consoleSink);

        // Create window and graphics device (one line!)
        WindowCreateInfo windowCI = new WindowCreateInfo
        {
            X = 100,
            Y = 100,
            WindowWidth = 1920,
            WindowHeight = 1080,
            WindowTitle = "Alina's Rail Tools - Terrain Editor",
            WindowInitialState = WindowState.Maximized
        };

        GraphicsDeviceOptions options = new GraphicsDeviceOptions
        {
            PreferStandardClipSpaceYDirection = true,
            PreferDepthRangeZeroToOne = true,
            Debug = true,
            SwapchainDepthFormat = PixelFormat.R32_Float,  // Enable depth buffer on swapchain
            SwapchainSrgbFormat = false,
            SyncToVerticalBlank = true  // VSync to reduce tearing
        };

        VeldridStartup.CreateWindowAndGraphicsDevice(
            windowCI,
            options,
            GraphicsBackend.Vulkan,
            out _window,
            out _graphicsDevice);

        Console.WriteLine($"[Program] Graphics device created: {_graphicsDevice.BackendType}");
        Console.WriteLine($"[Program] Window: {_window.Width}x{_window.Height}");

        // Create resources first so we have terrain manager and ECS world
        CreateResources();

        // Create camera entity in ECS (looking at tile 1,1 from above)
        float aspectRatio = (float)_window.Width / _window.Height;
        float tileDim = VeldridTerrainManager.TileDimension;
        float tileCenter = tileDim + tileDim / 2;
        Vector3 cameraPos = new Vector3(tileCenter, 800, tileCenter - 500);
        Vector3 cameraTarget = new Vector3(tileCenter, 500, tileCenter);

        var cameraComponent = new CameraComponent(cameraPos, cameraTarget, aspectRatio);
        var orbitComponent = new OrbitCameraComponent(cameraTarget, yaw: 0.0f, pitch: 45.0f, distance: 1000.0f);
        var keyStateComponent = new KeyStateComponent();

        _world!.Create(cameraComponent, orbitComponent, keyStateComponent);
        Console.WriteLine($"[Program] Camera entity created in ECS");

        // Main loop
        while (_window.Exists)
        {
            if (_window.Exists)
            {
                // Start frame timing
                _frameStopwatch.Restart();

                Update();
                Draw();

                // Stop frame timing and check for slow frames
                _frameStopwatch.Stop();
                double frameTimeMs = _frameStopwatch.Elapsed.TotalMilliseconds;
                _currentFrameTimeMs = frameTimeMs;

                // Calculate instantaneous FPS and smooth it
                double instantFPS = frameTimeMs > 0 ? 1000.0 / frameTimeMs : 60.0;
                _smoothedFPS = _smoothedFPS * 0.95 + instantFPS * 0.05; // Exponential moving average

                if (frameTimeMs > _slowFrameThresholdMs)
                {
                    Log.Warning("[Performance] Slow frame detected: {FrameTime:F2}ms (threshold: {Threshold:F1}ms, {FPS:F1} FPS)",
                        frameTimeMs, _slowFrameThresholdMs, instantFPS);
                }
            }
        }

        // Cleanup
        DisposeResources();
        _graphicsDevice.Dispose();
    }

    private static void CreateResources()
    {
        Console.WriteLine($"[Program] Swapchain has depth buffer: {_graphicsDevice!.SwapchainFramebuffer.DepthTarget != null}");

        // Create antialiasing manager (configurable AA type)
        _aaManager = new AntialiasingManager(_graphicsDevice!, AntialiasingType.MSAA_4x);

        // Create ECS World
        _world = World.Create();
        Console.WriteLine("[Program] ECS World created");

        // Create mesh manager for centralized mesh lifecycle management
        _meshManager = new MeshManager(_graphicsDevice!);

        // Create terrain manager and initialize with AA output description
        _terrainManager = new VeldridTerrainManager(_graphicsDevice!);
        _terrainManager.Initialize(_aaManager.OutputDescription);

        // Start async tile discovery (will populate tile cache in background)
        _terrainManager.PrelocateTilesAsync();

        // Create game graph manager (for rendering resources only)
        _graphManager = new GameGraphManager(_graphicsDevice!);
        string gameRootDir = _terrainManager.GetGameRootDirectory();

        // Initialize arrow mesh and rendering resources (use AA output description)
        _graphManager.InitializeRenderingOnly(_aaManager.OutputDescription);

        // Create database and async loader (will load base game + mods)
        _database = new WorkingStateDB();
        _asyncLoader = new AsyncGraphLoader(_database, gameRootDir);

        // Get rendering resources
        var (pipeline, uniformLayout, arrowMesh) = _graphManager.GetRenderingResources();

        // Create SystemContainer and register all services and systems
        Console.WriteLine("[Program] Setting up dependency injection container...");
        var container = new SystemContainer(_world);

        // Register services (singletons)
        container.RegisterSingleton(_graphicsDevice!);
        container.RegisterSingleton(_meshManager);
        container.RegisterSingleton(_database);
        container.RegisterSingleton(pipeline);
        container.RegisterSingleton(uniformLayout);
        container.RegisterSingleton(arrowMesh);
        container.RegisterSingleton(_aaManager.OutputDescription);
        container.RegisterSingleton(_terrainManager);
        container.RegisterSingleton(20); // Terrain tile load radius (matches old VeldridTerrainManager default)

        // Register systems (container will auto-detect dependencies and order them)
        container.RegisterSystem<ICameraSystem, CameraSystem>();
        container.RegisterSystem<IDatabaseSyncSystem, DatabaseSyncSystem>();
        container.RegisterSystem<ICurveUpdateSystem, CurveUpdateSystem>();
        container.RegisterSystem<ICurveMeshGenerationSystem, CurveMeshGenerationSystem>();
        container.RegisterSystem<ILODSystem, LODSystem>();
        container.RegisterSystem<ITrackRenderSystem, RenderSystem>();
        container.RegisterSystem<ITieRenderingSystem, TieRenderingSystem>();
        container.RegisterSystem<IInstancedRenderSystem, InstancedRenderSystem>();
        container.RegisterSystem<ITerrainTileSystem, TerrainTileSystem>();
        container.RegisterSystem<ITerrainRenderingSystem, TerrainRenderingSystem>();

        // Build SystemManager (instantiates systems in dependency order)
        _systemManager = container.Build();

        // Create performance stats window (requires SystemManager)
        _performanceStatsWindow = new PerformanceStatsWindow(_systemManager);

        // Create test geometry (for debugging rendering) - DISABLED
        // var testGeometry = new TestGeometrySystem(_world, _meshManager, _graphicsDevice!);

        // Start async loading
        _asyncLoader.StartLoading();
        Console.WriteLine("[Program] Started async graph loading");

        _commandList = _graphicsDevice!.ResourceFactory.CreateCommandList();

        // Create ImGui controller for UI overlay (use swapchain - ImGui always renders to swapchain)
        _imguiController = new ImGuiController(
            _graphicsDevice!,
            _graphicsDevice.SwapchainFramebuffer.OutputDescription,
            _window!.Width,
            _window.Height);

        Console.WriteLine("[Program] Resources created");
    }

    private static void Update()
    {
        var updateStopwatch = Stopwatch.StartNew();

        // Calculate delta time
        double currentTime = _stopwatch.Elapsed.TotalSeconds;
        float deltaTime = (float)(currentTime - _lastFrameTime);
        _lastFrameTime = currentTime;

        // Get input snapshot and update ImGui
        InputSnapshot snapshot = _window!.PumpEvents();
        _imguiController?.Update(deltaTime, snapshot);

        // Check for console toggle (grave/tilde key) and performance window toggle (F1 key)
        foreach (var keyEvent in snapshot.KeyEvents)
        {
            if (keyEvent.Down && keyEvent.Key == Veldrid.Key.Tilde)
            {
                _debugConsole?.Toggle();
            }
            else if (keyEvent.Down && keyEvent.Key == Veldrid.Key.F1)
            {
                _performanceStatsWindow?.Toggle();
            }
        }

        // Only update camera if UI doesn't want input
        var io = ImGui.GetIO();
        if (!io.WantCaptureMouse && !io.WantCaptureKeyboard)
        {
            _systemManager?.UpdateCamera(snapshot, deltaTime);
        }

        // Update all ECS systems (handles database sync, curve updates, and mesh generation)
        var ecsUpdateStart = updateStopwatch.Elapsed.TotalMilliseconds;
        _systemManager?.Update();
        var ecsUpdateTime = updateStopwatch.Elapsed.TotalMilliseconds - ecsUpdateStart;

        updateStopwatch.Stop();
        var totalUpdateTime = updateStopwatch.Elapsed.TotalMilliseconds;

        // Log if Update took too long (only log if contributing to < 30 FPS)
        // Allow up to 20ms for update phase before warning
        if (totalUpdateTime > 20.0)
        {
            Log.Warning("[Performance] Slow Update: {UpdateTime:F2}ms (ECS: {ECSTime:F2}ms)",
                totalUpdateTime, ecsUpdateTime);
        }
    }

    private static void Draw()
    {
        var drawStopwatch = Stopwatch.StartNew();

        _commandList!.Begin();

        // Render to MSAA framebuffer (or swapchain if AA disabled)
        var renderFramebuffer = _aaManager!.GetRenderFramebuffer();
        _commandList.SetFramebuffer(renderFramebuffer);
        _commandList.ClearColorTarget(0, new RgbaFloat(0.53f, 0.81f, 0.92f, 1.0f));
        _commandList.ClearDepthStencil(1.0f);

        // Render all ECS entities (terrain, track nodes, rails, and ties)
        var renderStart = drawStopwatch.Elapsed.TotalMilliseconds;
        _systemManager?.Render(_commandList);
        var renderTime = drawStopwatch.Elapsed.TotalMilliseconds - renderStart;

        // Resolve MSAA to swapchain if needed
        if (_aaManager.RequiresResolve)
        {
            _aaManager.ResolveToSwapchain(_commandList);
        }

        // Render UI overlay to swapchain
        _commandList.SetFramebuffer(_graphicsDevice!.SwapchainFramebuffer);
        var uiStart = drawStopwatch.Elapsed.TotalMilliseconds;
        DrawUI();
        _imguiController?.Render(_graphicsDevice!, _commandList);
        var uiTime = drawStopwatch.Elapsed.TotalMilliseconds - uiStart;

        _commandList.End();

        var submitStart = drawStopwatch.Elapsed.TotalMilliseconds;
        _graphicsDevice.SubmitCommands(_commandList);
        _graphicsDevice.SwapBuffers();
        var submitTime = drawStopwatch.Elapsed.TotalMilliseconds - submitStart;

        drawStopwatch.Stop();
        var totalDrawTime = drawStopwatch.Elapsed.TotalMilliseconds;

        // Log if Draw took too long (only log if contributing to < 30 FPS)
        // Allow up to 20ms for draw phase before warning
        if (totalDrawTime > 20.0)
        {
            Log.Warning("[Performance] Slow Draw: {DrawTime:F2}ms (Render: {RenderTime:F2}ms, UI: {UITime:F2}ms, Submit: {SubmitTime:F2}ms)",
                totalDrawTime, renderTime, uiTime, submitTime);
        }
    }

    private static void DrawUI()
    {
        // Render debug console and system debug windows
        _debugConsole?.Render();
        _systemDebugWindow?.Render();
        _performanceStatsWindow?.Render();

        // Check if loading just completed with errors and show popup
        if (_asyncLoader != null && _asyncLoader.IsComplete && !_hasShownModErrorPopup)
        {
            var result = _asyncLoader.LoadOrderResult;
            if (result != null && result.Errors.Count > 0)
            {
                _showModErrorPopup = true;
            }
            _hasShownModErrorPopup = true;
        }

        // Show mod error popup if requested
        if (_showModErrorPopup)
        {
            DrawModErrorPopup();
        }

        // Get camera target from ECS
        Camera? camera = _systemManager?.CameraSystem.GetActiveCamera(_world!);
        if (camera == null)
            return;

        // Camera target is in Unity space (+Z forward)
        Vector3 cameraTarget = camera.Target;
        int tileX = (int)Math.Floor(cameraTarget.X / VeldridTerrainManager.TileDimension);
        int tileZ = (int)Math.Floor(cameraTarget.Z / VeldridTerrainManager.TileDimension);

        // Top right info panel
        ImGui.SetNextWindowPos(new Vector2(_window!.Width - 250, 10), ImGuiCond.Always);
        ImGui.SetNextWindowSize(new Vector2(240, 290), ImGuiCond.Always);
        ImGui.Begin("Info", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove);

        // Performance info
        Vector4 fpsColor = _smoothedFPS >= 55.0 ? new Vector4(0.2f, 1.0f, 0.2f, 1.0f) : // Green (good)
                           _smoothedFPS >= 30.0 ? new Vector4(1.0f, 0.8f, 0.2f, 1.0f) : // Yellow (ok)
                           new Vector4(1.0f, 0.2f, 0.2f, 1.0f); // Red (bad)
        ImGui.TextColored(fpsColor, $"FPS: {_smoothedFPS:F1}");
        ImGui.Text($"Frame: {_currentFrameTimeMs:F2}ms");
        ImGui.Separator();

        // Loading status
        if (_asyncLoader != null && !_asyncLoader.IsComplete)
        {
            ImGui.Text("Loading...");
            ImGui.Text($"Nodes: {_asyncLoader.NodesLoaded}");
            ImGui.Text($"Segments: {_asyncLoader.SegmentsLoaded}");
            ImGui.Separator();
        }

        // Mesh generation progress
        if (_systemManager?.CurveMeshGenerationSystem != null)
        {
            int pendingMeshes = _systemManager.CurveMeshGenerationSystem.GetPendingMeshCount();
            if (pendingMeshes > 0)
            {
                ImGui.TextColored(new Vector4(1.0f, 0.8f, 0.2f, 1.0f), "Generating Meshes...");
                ImGui.Text($"Pending: {pendingMeshes}");
                ImGui.Separator();
            }
        }

        // Compass
        ImGui.Text("Compass");
        ImGui.Separator();
        var drawList = ImGui.GetWindowDrawList();
        var compassCenter = new Vector2(ImGui.GetCursorScreenPos().X + 60, ImGui.GetCursorScreenPos().Y + 30);
        float compassRadius = 25.0f;

        // Draw compass circle
        drawList.AddCircle(compassCenter, compassRadius, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1)), 32, 2.0f);

        // Calculate camera yaw from camera position
        Vector3 cameraPos = camera.Position;
        Vector3 forward = Vector3.Normalize(cameraTarget - cameraPos);
        float yaw = MathF.Atan2(forward.X, forward.Z) * 180.0f / MathF.PI;

        // Draw cardinal directions (rotated by camera yaw)
        DrawCardinal(drawList, compassCenter, compassRadius, -yaw, 0, "N");
        DrawCardinal(drawList, compassCenter, compassRadius, -yaw, 90, "E");
        DrawCardinal(drawList, compassCenter, compassRadius, -yaw, 180, "S");
        DrawCardinal(drawList, compassCenter, compassRadius, -yaw, 270, "W");

        ImGui.Dummy(new Vector2(0, 70)); // Space for compass

        ImGui.Separator();
        ImGui.Text($"World: {cameraTarget.X:F1}, {cameraTarget.Y:F1}, {cameraTarget.Z:F1}");
        ImGui.Text($"Tile: ({tileX}, {tileZ})");

        ImGui.End();
    }

    private static void DrawCardinal(dynamic drawList, Vector2 center, float radius, float cameraYaw, float directionAngle, string label)
    {
        float angleRad = (directionAngle + cameraYaw) * MathF.PI / 180.0f;
        Vector2 pos = new Vector2(
            center.X + MathF.Sin(angleRad) * radius,
            center.Y + MathF.Cos(angleRad) * radius  // Fixed: + instead of - to correct compass orientation
        );

        // Draw text centered at position
        var textSize = ImGui.CalcTextSize(label);
        drawList.AddText(
            new Vector2(pos.X - textSize.X / 2, pos.Y - textSize.Y / 2),
            ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 0, 1)),
            label
        );
    }

    private static void DrawModErrorPopup()
    {
        var result = _asyncLoader?.LoadOrderResult;
        if (result == null || result.Errors.Count == 0)
        {
            _showModErrorPopup = false;
            return;
        }

        // Center the popup
        ImGui.SetNextWindowPos(new Vector2(_window!.Width / 2, _window.Height / 2), ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSize(new Vector2(600, 400), ImGuiCond.Appearing);

        if (ImGui.Begin("Mod Loading Errors", ref _showModErrorPopup, ImGuiWindowFlags.NoCollapse))
        {
            ImGui.TextWrapped($"Encountered {result.Errors.Count} error(s) while loading mods:");
            ImGui.Separator();
            ImGui.Spacing();

            // Create scrollable region for errors
            ImGui.BeginChild("ErrorList", new Vector2(0, -30), true);

            foreach (var error in result.Errors)
            {
                switch (error.Type)
                {
                    case AlinasRailTools.Shared.ModLoading.ModLoadErrorType.CircularDependency:
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.4f, 0.4f, 1.0f)); // Red
                        ImGui.TextWrapped($"• Circular Dependency:");
                        ImGui.PopStyleColor();
                        if (error.Cycle != null)
                        {
                            ImGui.Indent();
                            ImGui.TextWrapped(string.Join(" → ", error.Cycle));
                            ImGui.Unindent();
                        }
                        break;

                    case AlinasRailTools.Shared.ModLoading.ModLoadErrorType.MissingDependency:
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.8f, 0.2f, 1.0f)); // Yellow
                        ImGui.TextWrapped($"• Missing Dependency:");
                        ImGui.PopStyleColor();
                        ImGui.Indent();
                        ImGui.TextWrapped($"'{error.ModId}' requires '{error.RelatedModId}'");
                        ImGui.Unindent();
                        break;

                    case AlinasRailTools.Shared.ModLoading.ModLoadErrorType.Conflict:
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.6f, 0.2f, 1.0f)); // Orange
                        ImGui.TextWrapped($"• Mod Conflict:");
                        ImGui.PopStyleColor();
                        ImGui.Indent();
                        ImGui.TextWrapped($"'{error.ModId}' conflicts with '{error.RelatedModId}'");
                        ImGui.Unindent();
                        break;
                }

                ImGui.Spacing();
            }

            ImGui.EndChild();

            // Buttons at bottom
            ImGui.Spacing();
            if (ImGui.Button("Copy to Clipboard", new Vector2(150, 0)))
            {
                // Build error text for clipboard
                var errorText = new System.Text.StringBuilder();
                errorText.AppendLine($"Mod Loading Errors ({result.Errors.Count} total):");
                errorText.AppendLine();

                foreach (var error in result.Errors)
                {
                    switch (error.Type)
                    {
                        case AlinasRailTools.Shared.ModLoading.ModLoadErrorType.CircularDependency:
                            errorText.AppendLine("• Circular Dependency:");
                            if (error.Cycle != null)
                            {
                                errorText.AppendLine($"  {string.Join(" → ", error.Cycle)}");
                            }
                            break;

                        case AlinasRailTools.Shared.ModLoading.ModLoadErrorType.MissingDependency:
                            errorText.AppendLine("• Missing Dependency:");
                            errorText.AppendLine($"  '{error.ModId}' requires '{error.RelatedModId}'");
                            break;

                        case AlinasRailTools.Shared.ModLoading.ModLoadErrorType.Conflict:
                            errorText.AppendLine("• Mod Conflict:");
                            errorText.AppendLine($"  '{error.ModId}' conflicts with '{error.RelatedModId}'");
                            break;
                    }
                    errorText.AppendLine();
                }

                ImGui.SetClipboardText(errorText.ToString());
            }

            ImGui.SameLine();
            if (ImGui.Button("Close", new Vector2(120, 0)))
            {
                _showModErrorPopup = false;
            }

            ImGui.End();
        }
    }

    private static void DisposeResources()
    {
        _terrainManager?.Dispose();
        _graphManager?.Dispose();
        _imguiController?.Dispose();
        _commandList?.Dispose();
        _systemManager?.Dispose();
        _world?.Dispose();
        _meshManager?.Dispose();
        _aaManager?.Dispose();
    }
}
