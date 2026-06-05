using AlinasRailTools.Shared.Definitions;
using System.Numerics;

namespace AlinasRailTools.Editor.Track;

/// <summary>
/// Runtime representation of a track segment connecting two nodes
/// </summary>
public class TrackSegmentData
{
    /// <summary>
    /// Unique identifier for this segment
    /// </summary>
    public string Id { get; set; } = "";

    /// <summary>
    /// Start node ID
    /// </summary>
    public string StartId { get; set; } = "";

    /// <summary>
    /// End node ID
    /// </summary>
    public string EndId { get; set; } = "";

    /// <summary>
    /// Segment priority (for routing)
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Group ID (for track groups)
    /// </summary>
    public string GroupId { get; set; } = "";

    /// <summary>
    /// Visual style (standard, tunnel, bridge)
    /// </summary>
    public TrackSegmentStyle Style { get; set; }

    /// <summary>
    /// Computed curve points for rendering
    /// Generated from start/end node positions and tangents
    /// </summary>
    public Vector3[] CurvePoints { get; set; } = Array.Empty<Vector3>();

    /// <summary>
    /// Generate curve points from start/end nodes using cubic Bezier curve
    /// Uses node rotations to determine tangent directions
    /// </summary>
    public void GenerateCurvePoints(TrackNodeData startNode, TrackNodeData endNode, int resolution = 20)
    {
        CurvePoints = new Vector3[resolution];

        Vector3 p0 = startNode.Position;
        Vector3 p3 = endNode.Position;

        // Calculate tangent length as a fraction of distance between nodes
        float distance = Vector3.Distance(p0, p3);
        float tangentLength = distance * 0.33f; // 1/3 of distance works well for most curves

        // Convert node rotations (Euler angles in degrees) to forward direction vectors
        Vector3 p1 = p0 + GetForwardFromRotation(startNode.Rotation) * tangentLength;
        Vector3 p2 = p3 - GetForwardFromRotation(endNode.Rotation) * tangentLength;

        // Generate points along cubic Bezier curve
        for (int i = 0; i < resolution; i++)
        {
            float t = i / (float)(resolution - 1);
            CurvePoints[i] = EvaluateCubicBezier(p0, p1, p2, p3, t);
        }
    }

    /// <summary>
    /// Convert Euler rotation (degrees) to forward direction vector
    /// Unity uses Y-up, right-handed coordinate system
    /// </summary>
    private Vector3 GetForwardFromRotation(Vector3 eulerDegrees)
    {
        // Convert degrees to radians
        float yaw = eulerDegrees.Y * MathF.PI / 180.0f;
        float pitch = eulerDegrees.X * MathF.PI / 180.0f;

        // Calculate forward vector from yaw and pitch
        // Unity forward is typically +Z, but track rotation may differ
        return new Vector3(
            MathF.Sin(yaw) * MathF.Cos(pitch),
            -MathF.Sin(pitch),
            MathF.Cos(yaw) * MathF.Cos(pitch)
        );
    }

    /// <summary>
    /// Evaluate cubic Bezier curve at parameter t
    /// B(t) = (1-t)³P₀ + 3(1-t)²tP₁ + 3(1-t)t²P₂ + t³P₃
    /// </summary>
    private Vector3 EvaluateCubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1.0f - t;
        float uu = u * u;
        float uuu = uu * u;
        float tt = t * t;
        float ttt = tt * t;

        Vector3 point = uuu * p0;           // (1-t)³P₀
        point += 3 * uu * t * p1;           // 3(1-t)²tP₁
        point += 3 * u * tt * p2;           // 3(1-t)t²P₂
        point += ttt * p3;                  // t³P₃

        return point;
    }
}
