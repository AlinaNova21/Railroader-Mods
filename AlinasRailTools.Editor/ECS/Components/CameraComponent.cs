using System.Numerics;
using AlinasRailTools.Editor.Rendering;

namespace AlinasRailTools.Editor.ECS.Components;

/// <summary>
/// Component that defines camera properties for rendering
/// Replaces the traditional Camera class with an ECS-friendly component
/// </summary>
public struct CameraComponent
{
    public Vector3 Position;
    public Vector3 Target;
    public Vector3 Up;
    public float Fov;
    public float AspectRatio;
    public float NearPlane;
    public float FarPlane;

    /// <summary>
    /// Tag to mark the active camera (only one should be active at a time)
    /// </summary>
    public bool IsActive;

    public CameraComponent(Vector3 position, Vector3 target, float aspectRatio)
    {
        Position = position;
        Target = target;
        Up = Vector3.UnitY;
        Fov = 45.0f;
        AspectRatio = aspectRatio;
        NearPlane = 0.1f;
        FarPlane = 50000.0f;
        IsActive = true;
    }

    /// <summary>
    /// Get a Camera object for compatibility with existing rendering code
    /// This allows gradual migration - systems can still use Camera objects
    /// </summary>
    public readonly Camera ToCamera()
    {
        return new Camera(Position, Target, AspectRatio)
        {
            Up = Up,
            Fov = Fov,
            NearPlane = NearPlane,
            FarPlane = FarPlane
        };
    }
}
