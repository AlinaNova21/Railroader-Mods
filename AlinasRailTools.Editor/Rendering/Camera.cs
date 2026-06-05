using System.Numerics;

namespace AlinasRailTools.Editor.Rendering;

/// <summary>
/// Axis-aligned bounding box for frustum culling
/// </summary>
public struct BoundingBox
{
    public Vector3 Min;
    public Vector3 Max;

    public BoundingBox(Vector3 min, Vector3 max)
    {
        Min = min;
        Max = max;
    }

    /// <summary>
    /// Create bounding box from a set of points
    /// </summary>
    public static BoundingBox FromPoints(IEnumerable<Vector3> points)
    {
        var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        foreach (var point in points)
        {
            min = Vector3.Min(min, point);
            max = Vector3.Max(max, point);
        }

        return new BoundingBox(min, max);
    }

    /// <summary>
    /// Expand bounding box by a margin on all sides
    /// </summary>
    public BoundingBox Expand(float margin)
    {
        return new BoundingBox(
            Min - new Vector3(margin, margin, margin),
            Max + new Vector3(margin, margin, margin)
        );
    }

    /// <summary>
    /// Transform bounding box from Unity space (+Z forward) to Veldrid space (-Z forward)
    /// This flips the Z coordinates for proper frustum culling
    /// </summary>
    public BoundingBox ToVeldridSpace()
    {
        // Flip Z coordinates
        var newMin = new Vector3(Min.X, Min.Y, -Min.Z);
        var newMax = new Vector3(Max.X, Max.Y, -Max.Z);

        // After flipping, ensure min/max are correct (Z values swap)
        return new BoundingBox(
            new Vector3(
                Math.Min(newMin.X, newMax.X),
                Math.Min(newMin.Y, newMax.Y),
                Math.Min(newMin.Z, newMax.Z)
            ),
            new Vector3(
                Math.Max(newMin.X, newMax.X),
                Math.Max(newMin.Y, newMax.Y),
                Math.Max(newMin.Z, newMax.Z)
            )
        );
    }
}

/// <summary>
/// View frustum for culling out-of-view objects
/// </summary>
public struct Frustum
{
    // 6 frustum planes (left, right, bottom, top, near, far)
    public Plane Left;
    public Plane Right;
    public Plane Bottom;
    public Plane Top;
    public Plane Near;
    public Plane Far;

    /// <summary>
    /// Test if a bounding box intersects the frustum
    /// Returns true if the box is fully or partially inside the frustum
    /// </summary>
    public bool Intersects(BoundingBox box)
    {
        // Test against each plane
        if (!IntersectsPlane(Left, box)) return false;
        if (!IntersectsPlane(Right, box)) return false;
        if (!IntersectsPlane(Bottom, box)) return false;
        if (!IntersectsPlane(Top, box)) return false;
        if (!IntersectsPlane(Near, box)) return false;
        if (!IntersectsPlane(Far, box)) return false;

        return true;
    }

    private bool IntersectsPlane(Plane plane, BoundingBox box)
    {
        // Get the positive vertex (furthest point in direction of plane normal)
        Vector3 positiveVertex = new Vector3(
            plane.Normal.X >= 0 ? box.Max.X : box.Min.X,
            plane.Normal.Y >= 0 ? box.Max.Y : box.Min.Y,
            plane.Normal.Z >= 0 ? box.Max.Z : box.Min.Z
        );

        // If the positive vertex is behind the plane, the box is completely outside
        return Plane.DotCoordinate(plane, positiveVertex) >= 0;
    }
}

/// <summary>
/// 3D camera for rendering
/// </summary>
public class Camera
{
    public Vector3 Position { get; set; }
    public Vector3 Target { get; set; }
    public Vector3 Up { get; set; } = Vector3.UnitY;
    public float Fov { get; set; } = 45.0f;
    public float AspectRatio { get; set; }
    public float NearPlane { get; set; } = 0.1f;
    public float FarPlane { get; set; } = 50000.0f;

    public Camera(Vector3 position, Vector3 target, float aspectRatio)
    {
        Position = position;
        Target = target;
        AspectRatio = aspectRatio;
    }

    public Matrix4x4 GetViewMatrix()
    {
        return Matrix4x4.CreateLookAt(Position, Target, Up);
    }

    public Matrix4x4 GetProjectionMatrix()
    {
        return Matrix4x4.CreatePerspectiveFieldOfView(
            Fov * (MathF.PI / 180.0f),
            AspectRatio,
            NearPlane,
            FarPlane
        );
    }

    public Matrix4x4 GetViewProjectionMatrix()
    {
        return GetViewMatrix() * GetProjectionMatrix();
    }

    /// <summary>
    /// Extract frustum planes from the view-projection matrix
    /// </summary>
    public Frustum GetFrustum()
    {
        Matrix4x4 vp = GetViewProjectionMatrix();
        Frustum frustum = new Frustum();

        // Extract frustum planes from view-projection matrix
        // Left plane: row4 + row1
        frustum.Left = Plane.Normalize(new Plane(
            vp.M14 + vp.M11,
            vp.M24 + vp.M21,
            vp.M34 + vp.M31,
            vp.M44 + vp.M41
        ));

        // Right plane: row4 - row1
        frustum.Right = Plane.Normalize(new Plane(
            vp.M14 - vp.M11,
            vp.M24 - vp.M21,
            vp.M34 - vp.M31,
            vp.M44 - vp.M41
        ));

        // Bottom plane: row4 + row2
        frustum.Bottom = Plane.Normalize(new Plane(
            vp.M14 + vp.M12,
            vp.M24 + vp.M22,
            vp.M34 + vp.M32,
            vp.M44 + vp.M42
        ));

        // Top plane: row4 - row2
        frustum.Top = Plane.Normalize(new Plane(
            vp.M14 - vp.M12,
            vp.M24 - vp.M22,
            vp.M34 - vp.M32,
            vp.M44 - vp.M42
        ));

        // Near plane: row4 + row3
        frustum.Near = Plane.Normalize(new Plane(
            vp.M14 + vp.M13,
            vp.M24 + vp.M23,
            vp.M34 + vp.M33,
            vp.M44 + vp.M43
        ));

        // Far plane: row4 - row3
        frustum.Far = Plane.Normalize(new Plane(
            vp.M14 - vp.M13,
            vp.M24 - vp.M23,
            vp.M34 - vp.M33,
            vp.M44 - vp.M43
        ));

        return frustum;
    }
}
