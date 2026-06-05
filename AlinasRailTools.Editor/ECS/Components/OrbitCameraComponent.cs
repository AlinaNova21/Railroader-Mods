using System.Numerics;
using Veldrid;

namespace AlinasRailTools.Editor.ECS.Components;

/// <summary>
/// Component that defines orbit camera control state
/// Manages yaw, pitch, distance, and input tracking for orbit camera behavior
/// </summary>
public struct OrbitCameraComponent
{
    // Orbit parameters
    public Vector3 Target;
    public float Yaw;      // Rotation around Y axis (degrees)
    public float Pitch;    // Looking down angle (degrees)
    public float Distance;

    // Input state (tracked frame-to-frame)
    public Vector2 LastMousePos;
    public bool MouseRightDown;

    // Movement settings (constants)
    public const float BaseMovementSpeed = 200.0f; // Base speed in units/second
    public const float MouseSensitivity = 0.2f;    // Degrees per pixel
    public const float BaseZoomSpeed = 100.0f;     // Base zoom speed
    public const float MinDistance = 10.0f;
    public const float MaxDistance = 20000.0f;
    public const float MinPitch = -89.0f;
    public const float MaxPitch = 89.0f;

    public OrbitCameraComponent(Vector3 target, float yaw, float pitch, float distance)
    {
        Target = target;
        Yaw = yaw;
        Pitch = pitch;
        Distance = distance;
        LastMousePos = Vector2.Zero;
        MouseRightDown = false;
    }
}

/// <summary>
/// Component to track pressed keys for continuous movement
/// Stored separately to avoid copying large HashSet in struct
/// </summary>
public class KeyStateComponent
{
    public HashSet<Key> KeysDown { get; } = new();
}
