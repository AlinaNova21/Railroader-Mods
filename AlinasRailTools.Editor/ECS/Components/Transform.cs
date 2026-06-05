using System.Numerics;

namespace AlinasRailTools.Editor.ECS.Components;

/// <summary>
/// Transform component containing position, rotation, and scale.
/// Used for spatial positioning of entities in 3D space.
///
/// Coordinate System: Uses Unity coordinates (+X right, +Y up, +Z forward)
/// The RenderSystem automatically converts to Veldrid coordinates (-Z forward) at render time.
/// </summary>
public struct Transform
{
    /// <summary>
    /// World space position
    /// </summary>
    public Vector3 Position;

    /// <summary>
    /// Rotation as a quaternion
    /// </summary>
    public Quaternion Rotation;

    /// <summary>
    /// Scale vector (uniform scale uses same value for X, Y, Z)
    /// </summary>
    public Vector3 Scale;

    public Transform(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        Position = position;
        Rotation = rotation;
        Scale = scale;
    }

    /// <summary>
    /// Create transform with position and rotation (uniform scale of 1)
    /// </summary>
    public Transform(Vector3 position, Quaternion rotation) : this(position, rotation, Vector3.One)
    {
    }

    /// <summary>
    /// Create transform with position only (identity rotation, uniform scale of 1)
    /// </summary>
    public Transform(Vector3 position) : this(position, Quaternion.Identity, Vector3.One)
    {
    }

    /// <summary>
    /// Create identity transform (origin, no rotation, scale of 1)
    /// </summary>
    public static Transform Identity => new(Vector3.Zero, Quaternion.Identity, Vector3.One);

    /// <summary>
    /// Create transform from Euler angles (degrees)
    /// </summary>
    public static Transform FromEuler(Vector3 position, Vector3 eulerDegrees, Vector3 scale)
    {
        // Convert degrees to radians
        float yawRad = eulerDegrees.Y * MathF.PI / 180.0f;
        float pitchRad = eulerDegrees.X * MathF.PI / 180.0f;
        float rollRad = eulerDegrees.Z * MathF.PI / 180.0f;

        // Create quaternion from Euler angles (YXZ order, matching Unity/game convention)
        var rotation = Quaternion.CreateFromYawPitchRoll(yawRad, pitchRad, rollRad);

        return new Transform(position, rotation, scale);
    }

    /// <summary>
    /// Calculate the model matrix (transforms local space to world space)
    /// Composes Scale, Rotation, and Translation into a single matrix
    /// Returns matrix in Unity coordinate space (+Z forward)
    /// </summary>
    public Matrix4x4 GetModelMatrix()
    {
        // Start with scale and rotation
        var matrix = Matrix4x4.CreateScale(Scale) * Matrix4x4.CreateFromQuaternion(Rotation);

        // Directly set translation components (avoid matrix multiplication on translation)
        matrix.M41 = Position.X;
        matrix.M42 = Position.Y;
        matrix.M43 = Position.Z;

        return matrix;
    }

    /// <summary>
    /// Calculate the model matrix in Veldrid coordinate space (-Z forward)
    /// This applies the coordinate system conversion automatically
    /// </summary>
    public Matrix4x4 GetVeldridMatrix()
    {
        // Coordinate conversion matrix: Unity (+Z forward) to Veldrid (-Z forward)
        Matrix4x4 unityToVeldrid = Matrix4x4.CreateScale(1, 1, -1);
        return GetModelMatrix() * unityToVeldrid;
    }

    /// <summary>
    /// Get forward direction vector (+Z in local space)
    /// </summary>
    public Vector3 Forward => Vector3.Transform(Vector3.UnitZ, Rotation);

    /// <summary>
    /// Get right direction vector (+X in local space)
    /// </summary>
    public Vector3 Right => Vector3.Transform(Vector3.UnitX, Rotation);

    /// <summary>
    /// Get up direction vector (+Y in local space)
    /// </summary>
    public Vector3 Up => Vector3.Transform(Vector3.UnitY, Rotation);
}
