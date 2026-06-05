using System.Numerics;
using Arch.Core;
using Arch.Core.Extensions;
using Serilog;
using Veldrid;
using AlinasRailTools.Editor.ECS.Components;
using AlinasRailTools.Editor.Rendering;

namespace AlinasRailTools.Editor.ECS.Systems;

/// <summary>
/// System that handles camera updates, including orbit controls and input processing
/// </summary>
public class CameraSystem : ICameraSystem
{
    private readonly ILogger _logger;
    private readonly ITerrainTileSystem? _terrainTileSystem;

    public CameraSystem(ITerrainTileSystem? terrainTileSystem = null)
    {
        _logger = Log.ForContext<CameraSystem>();
        _terrainTileSystem = terrainTileSystem;
        _logger.Information("Initialized");
    }

    /// <summary>
    /// Update camera based on input and orbit controls
    /// </summary>
    public void Update(World world, InputSnapshot input, float deltaTime)
    {
        // Query for active camera with orbit controls
        var query = new QueryDescription()
            .WithAll<CameraComponent, OrbitCameraComponent, KeyStateComponent>();

        world.Query(in query, (Entity entity, ref CameraComponent camera, ref OrbitCameraComponent orbit, ref KeyStateComponent keyState) =>
        {
            // Only process active cameras
            if (!camera.IsActive)
                return;

            // Process mouse orbit (right button)
            foreach (var mouseEvent in input.MouseEvents)
            {
                if (mouseEvent.Down && mouseEvent.MouseButton == MouseButton.Right)
                {
                    orbit.MouseRightDown = true;
                    orbit.LastMousePos = input.MousePosition;
                }
                else if (!mouseEvent.Down && mouseEvent.MouseButton == MouseButton.Right)
                {
                    orbit.MouseRightDown = false;
                }
            }

            if (orbit.MouseRightDown)
            {
                var currentMousePos = input.MousePosition;
                Vector2 delta = currentMousePos - orbit.LastMousePos;

                if (delta.LengthSquared() > 0.001f)
                {
                    orbit.Yaw += delta.X * OrbitCameraComponent.MouseSensitivity;
                    orbit.Pitch -= delta.Y * OrbitCameraComponent.MouseSensitivity;
                    orbit.Pitch = Math.Clamp(orbit.Pitch, OrbitCameraComponent.MinPitch, OrbitCameraComponent.MaxPitch);

                    orbit.LastMousePos = currentMousePos;
                    UpdateCameraPosition(ref camera, ref orbit);
                }
            }

            // Process mouse wheel zoom
            float wheelDelta = input.WheelDelta;
            if (Math.Abs(wheelDelta) > 0.001f)
            {
                // Zoom speed scales with distance (slower when closer)
                float zoomScale = MathF.Max(0.1f, orbit.Distance / 1000.0f);
                float zoomAmount = wheelDelta * OrbitCameraComponent.BaseZoomSpeed * zoomScale;

                orbit.Distance -= zoomAmount;
                orbit.Distance = Math.Clamp(orbit.Distance, OrbitCameraComponent.MinDistance, OrbitCameraComponent.MaxDistance);
                UpdateCameraPosition(ref camera, ref orbit);
            }

            // Track key state
            foreach (var keyEvent in input.KeyEvents)
            {
                if (keyEvent.Down)
                    keyState.KeysDown.Add(keyEvent.Key);
                else
                    keyState.KeysDown.Remove(keyEvent.Key);
            }

            // WASD movement (camera-relative)
            // Apply 10x speed multiplier if Shift is held
            float speedMultiplier = keyState.KeysDown.Contains(Key.ShiftLeft) || keyState.KeysDown.Contains(Key.ShiftRight) ? 10.0f : 1.0f;
            float movementSpeed = OrbitCameraComponent.BaseMovementSpeed * (orbit.Distance / 1000.0f) * deltaTime * speedMultiplier;

            // Calculate camera's forward and right vectors (on XZ plane)
            float yawRad = orbit.Yaw * MathF.PI / 180.0f;
            Vector3 forward = new Vector3(MathF.Sin(yawRad), 0, MathF.Cos(yawRad));
            Vector3 right = new Vector3(MathF.Cos(yawRad), 0, -MathF.Sin(yawRad));

            Vector3 movement = Vector3.Zero;

            // Check held keys for movement
            if (keyState.KeysDown.Contains(Key.W))
                movement -= forward;
            if (keyState.KeysDown.Contains(Key.S))
                movement += forward;
            if (keyState.KeysDown.Contains(Key.D))
                movement += right;
            if (keyState.KeysDown.Contains(Key.A))
                movement -= right;
            if (keyState.KeysDown.Contains(Key.Q))
                movement -= Vector3.UnitY; // Move down
            if (keyState.KeysDown.Contains(Key.E))
                movement += Vector3.UnitY; // Move up

            if (movement != Vector3.Zero)
            {
                movement = Vector3.Normalize(movement);
                orbit.Target += movement * movementSpeed;

                // Auto-adjust target height based on terrain (focal point 50m above surface)
                if (_terrainTileSystem != null)
                {
                    float terrainHeight = _terrainTileSystem.GetTerrainHeightAt(orbit.Target);
                    const float focalHeightAboveTerrain = 50.0f; // Focal point 50m above terrain

                    // Use fixed 500m height when zoomed out beyond 500m
                    float targetHeight;
                    if (orbit.Distance > 500.0f)
                    {
                        targetHeight = 500.0f;
                    }
                    else
                    {
                        targetHeight = terrainHeight + focalHeightAboveTerrain;
                    }

                    // Smoothly interpolate to target height (10% per frame = ~0.2s to settle)
                    float smoothingFactor = Math.Clamp(deltaTime * 5.0f, 0.0f, 1.0f);
                    float newHeight = orbit.Target.Y + (targetHeight - orbit.Target.Y) * smoothingFactor;

                    orbit.Target = new Vector3(orbit.Target.X, newHeight, orbit.Target.Z);
                }

                UpdateCameraPosition(ref camera, ref orbit);
            }

            // Allow camera to go below terrain for close-up views (no constraint)

            // Write back updated components
            world.Set(entity, camera);
            world.Set(entity, orbit);
        });
    }

    /// <summary>
    /// Update terrain tiles based on camera target position
    /// </summary>
    public void UpdateTerrainTiles(World world)
    {
        // Terrain tile system handles its own updates based on camera position
        // No need to manually trigger tile updates here
    }

    /// <summary>
    /// Get the active camera (for rendering systems)
    /// Returns null if no active camera found
    /// </summary>
    public Camera? GetActiveCamera(World world)
    {
        Camera? result = null;

        var query = new QueryDescription()
            .WithAll<CameraComponent>();

        world.Query(in query, (ref CameraComponent camera) =>
        {
            if (camera.IsActive && result == null)
            {
                result = camera.ToCamera();
            }
        });

        return result;
    }

    /// <summary>
    /// Update camera position based on orbit parameters
    /// </summary>
    private static void UpdateCameraPosition(ref CameraComponent camera, ref OrbitCameraComponent orbit)
    {
        // Calculate camera position from orbit parameters
        float yawRad = orbit.Yaw * MathF.PI / 180.0f;
        float pitchRad = orbit.Pitch * MathF.PI / 180.0f;

        float x = orbit.Distance * MathF.Cos(pitchRad) * MathF.Sin(yawRad);
        float y = orbit.Distance * MathF.Sin(pitchRad);
        float z = orbit.Distance * MathF.Cos(pitchRad) * MathF.Cos(yawRad);

        Vector3 newPosition = orbit.Target + new Vector3(x, y, z);

        camera.Position = newPosition;
        camera.Target = orbit.Target;
    }

    /// <summary>
    /// Get the active camera's target position
    /// </summary>
    public Vector3? GetCameraTarget(World world)
    {
        var camera = GetActiveCamera(world);
        return camera?.Target;
    }

    public void Dispose()
    {
        // No resources to dispose
    }
}
