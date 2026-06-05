using System.Numerics;
using AlinasRailTools.Editor.Rendering;

namespace AlinasRailTools.Editor.ECS.Components;

/// <summary>
/// Component storing a calculated bezier curve for a track segment
/// </summary>
public struct BezierCurveComponent
{
    /// <summary>
    /// The bezier curve data
    /// </summary>
    public BezierCurve? Curve;

    /// <summary>
    /// Cached start position (for dirty detection)
    /// </summary>
    public Vector3 CachedStartPosition;

    /// <summary>
    /// Cached end position (for dirty detection)
    /// </summary>
    public Vector3 CachedEndPosition;

    /// <summary>
    /// Cached start rotation (for dirty detection)
    /// </summary>
    public Quaternion CachedStartRotation;

    /// <summary>
    /// Cached end rotation (for dirty detection)
    /// </summary>
    public Quaternion CachedEndRotation;

    public BezierCurveComponent()
    {
        Curve = null;
        CachedStartPosition = Vector3.Zero;
        CachedEndPosition = Vector3.Zero;
        CachedStartRotation = Quaternion.Identity;
        CachedEndRotation = Quaternion.Identity;
    }
}
