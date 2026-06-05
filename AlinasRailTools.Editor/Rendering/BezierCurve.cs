using System.Numerics;

namespace AlinasRailTools.Editor.Rendering;

/// <summary>
/// Cubic Bezier curve implementation for track segments
/// </summary>
public class BezierCurve
{
    private readonly Vector3[] _controlPoints;
    private readonly Vector3 _startUp;
    private readonly Vector3 _endUp;

    public BezierCurve(Vector3[] controlPoints, Vector3 startUp, Vector3 endUp)
    {
        if (controlPoints.Length != 4)
            throw new ArgumentException("Bezier curve requires exactly 4 control points");

        _controlPoints = controlPoints;
        _startUp = startUp;
        _endUp = endUp;
    }

    /// <summary>
    /// Evaluate the bezier curve at parameter t (0 to 1)
    /// </summary>
    public Vector3 Evaluate(float t)
    {
        // Cubic bezier formula: (1-t)³P₀ + 3(1-t)²tP₁ + 3(1-t)t²P₂ + t³P₃
        float oneMinusT = 1.0f - t;
        float oneMinusT2 = oneMinusT * oneMinusT;
        float oneMinusT3 = oneMinusT2 * oneMinusT;
        float t2 = t * t;
        float t3 = t2 * t;

        return oneMinusT3 * _controlPoints[0] +
               3 * oneMinusT2 * t * _controlPoints[1] +
               3 * oneMinusT * t2 * _controlPoints[2] +
               t3 * _controlPoints[3];
    }

    /// <summary>
    /// Get the tangent (derivative) at parameter t
    /// </summary>
    public Vector3 GetTangent(float t)
    {
        // Derivative of cubic bezier: 3(1-t)²(P₁-P₀) + 6(1-t)t(P₂-P₁) + 3t²(P₃-P₂)
        float oneMinusT = 1.0f - t;
        float oneMinusT2 = oneMinusT * oneMinusT;
        float t2 = t * t;

        return 3 * oneMinusT2 * (_controlPoints[1] - _controlPoints[0]) +
               6 * oneMinusT * t * (_controlPoints[2] - _controlPoints[1]) +
               3 * t2 * (_controlPoints[3] - _controlPoints[2]);
    }

    /// <summary>
    /// Get up vector interpolated between start and end
    /// </summary>
    public Vector3 GetUpVector(float t)
    {
        return Vector3.Normalize(Vector3.Lerp(_startUp, _endUp, t));
    }

    /// <summary>
    /// Calculate bezier tangent factor based on angle between forward vectors
    /// This is from the original game code
    /// </summary>
    public static float BezierTangentFactorForTangents(Vector3 tangentA, Vector3 tangentB)
    {
        // Calculate angle between the two forward vectors
        float dot = Vector3.Dot(tangentA, tangentB);
        dot = Math.Clamp(dot, -1.0f, 1.0f);
        float angle = MathF.Acos(dot);

        // Original game logic: factor decreases as angle increases
        // At 0 degrees (parallel): factor ≈ 0.5
        // At 90 degrees: factor ≈ 0.35
        // At 180 degrees (opposite): factor ≈ 0.2
        return 0.5f - (angle / MathF.PI) * 0.3f;
    }

    /// <summary>
    /// Calculate bounding box for this curve by sampling points
    /// </summary>
    public BoundingBox GetBounds(int samples = 20)
    {
        var points = new List<Vector3>();

        // Sample points along the curve
        for (int i = 0; i <= samples; i++)
        {
            float t = i / (float)samples;
            points.Add(Evaluate(t));
        }

        // Also include all control points (control points can extend beyond curve)
        points.AddRange(_controlPoints);

        return BoundingBox.FromPoints(points);
    }
}
