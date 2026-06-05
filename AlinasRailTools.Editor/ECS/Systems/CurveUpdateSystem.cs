using System.Numerics;
using Arch.Core;
using Arch.Core.Extensions;
using Serilog;
using AlinasRailTools.Editor.ECS.Components;
using AlinasRailTools.Editor.Rendering;

namespace AlinasRailTools.Editor.ECS.Systems;

/// <summary>
/// System that updates bezier curves for track segments when nodes move or rotate
/// </summary>
public class CurveUpdateSystem : ICurveUpdateSystem
{
    private readonly ILogger _logger;

    public CurveUpdateSystem()
    {
        _logger = Log.ForContext<CurveUpdateSystem>();
    }
    /// <summary>
    /// Update all curves that are dirty or whose nodes have changed
    /// </summary>
    public void Update(World world)
    {
        // Query all segments with curves
        var query = new QueryDescription()
            .WithAll<TrackSegmentComponent, BezierCurveComponent>();

        world.Query(in query, (Entity entity, ref TrackSegmentComponent segment, ref BezierCurveComponent curveComp) =>
        {
            // Check if segment references valid entities
            if (!world.IsAlive(segment.StartNodeEntity) || !world.IsAlive(segment.EndNodeEntity))
            {
                _logger.Warning("Segment {SegmentId} has invalid node references", segment.SegmentId);
                return;
            }

            // Get start and end node transforms
            if (!world.TryGet(segment.StartNodeEntity, out Transform startTransform) ||
                !world.TryGet(segment.EndNodeEntity, out Transform endTransform))
            {
                _logger.Warning("Segment {SegmentId} nodes missing Transform component", segment.SegmentId);
                return;
            }

            // Check if nodes have moved or rotated
            bool isDirty = segment.IsDirty ||
                           curveComp.CachedStartPosition != startTransform.Position ||
                           curveComp.CachedEndPosition != endTransform.Position ||
                           curveComp.CachedStartRotation != startTransform.Rotation ||
                           curveComp.CachedEndRotation != endTransform.Rotation;

            if (!isDirty)
                return;

            // Recalculate curve
            curveComp.Curve = CreateBezierCurve(startTransform, endTransform);

            // Update cache
            curveComp.CachedStartPosition = startTransform.Position;
            curveComp.CachedEndPosition = endTransform.Position;
            curveComp.CachedStartRotation = startTransform.Rotation;
            curveComp.CachedEndRotation = endTransform.Rotation;

            // Mark as clean
            segment.IsDirty = false;
        });
    }

    /// <summary>
    /// Create a bezier curve from two node transforms
    /// Uses Unity coordinate space (+Z forward)
    /// Matches game's CreateBezier and TangentPointAlongSegment logic
    /// </summary>
    private BezierCurve CreateBezierCurve(Transform startTransform, Transform endTransform)
    {
        Vector3 startPos = startTransform.Position;
        Vector3 endPos = endTransform.Position;

        // Calculate tangent magnitude using game's formula
        // Use the transform's forward vectors to get the angle between them
        Vector3 startForward = startTransform.Forward;
        Vector3 endForward = endTransform.Forward;

        float distance = (startPos - endPos).Length();
        float tangentFactor = BezierCurve.BezierTangentFactorForTangents(startForward, endForward);
        float tangentMagnitude = distance * tangentFactor;

        // Calculate tangent points using TangentPointAlongSegment logic:
        // Check which direction (forward or back) is closer to the other node
        Vector3 startForwardPoint = startPos + startForward * tangentMagnitude;
        Vector3 startBackPoint = startPos - startForward * tangentMagnitude;

        float distStartForward = (startForwardPoint - endPos).Length();
        float distStartBack = (startBackPoint - endPos).Length();
        Vector3 startTangent = (distStartForward < distStartBack) ? startForwardPoint : startBackPoint;

        Vector3 endForwardPoint = endPos + endForward * tangentMagnitude;
        Vector3 endBackPoint = endPos - endForward * tangentMagnitude;

        float distEndForward = (endForwardPoint - startPos).Length();
        float distEndBack = (endBackPoint - startPos).Length();
        Vector3 endTangent = (distEndForward < distEndBack) ? endForwardPoint : endBackPoint;

        // Up vectors (Unity Y-up)
        Vector3 up = Vector3.UnitY;

        return new BezierCurve(new[] { startPos, startTangent, endTangent, endPos }, up, up);
    }

    public void Dispose()
    {
        // No resources to dispose
    }
}
