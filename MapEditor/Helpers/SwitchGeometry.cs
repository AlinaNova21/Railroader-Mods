using System.Collections.Generic;
using Core;
using Track;
using UnityEngine;

namespace MapEditor.Helpers;

public static class SwitchGeometryProxy
{
  public static SwitchGeometry Calculate(TrackNode node, SegmentProxy a, SegmentProxy b, out SegmentProxy sliceA, out SegmentProxy sliceB, out List<SegmentProxy> remainder)
  {
    return SwitchGeometry.Calculate(node, a, b, out sliceA, out sliceB, out remainder);
  }

  public static LineCurve MakeGuardRail(LineCurve stockRail, LinePoint frogPoint)
  {
    var args = new object?[] { stockRail, frogPoint, null };
    var result = typeof(SwitchGeometry)
      .GetMethod("MakeGuardRail", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
      ?.Invoke(null, args);
    return (LineCurve)args[2]!;
  }

  public static void AlignSwitchCurves(SegmentProxy a, SegmentProxy b, out Vector3 origin, out BezierCurve aCurve, out BezierCurve bCurve)
  {
    var args = new object?[] { a, b, null, null, null };
    var result = typeof(SwitchGeometry)
      .GetMethod("AlignSwitchCurves", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
      ?.Invoke(null, args);
    origin = (Vector3)args[2]!;
    aCurve = (BezierCurve)args[3]!;
    bCurve = (BezierCurve)args[4]!;
  }

  public static SwitchGeometry.RailLineCurves MakeTrackLineSegments(BezierCurve center, Gauge gauge)
  {
    return SwitchGeometry.MakeTrackLineSegments(center, gauge);
  }

  public static bool Intersects(LineCurve aCurve, LineCurve bCurve, float frogDepth, out LinePoint intersection)
  {
    var args = new object?[] { aCurve, bCurve, frogDepth, null };
    var result = typeof(SwitchGeometry)
      .GetMethod("Intersects", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
      ?.Invoke(null, args);
    intersection = (LinePoint)args[3]!;
    return (bool)result!;
  }
}
