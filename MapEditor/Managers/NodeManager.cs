using System;
using System.Collections.Generic;
using System.Linq;
using MapEditor.StateTracker;
using MapEditor.StateTracker.Node;
using MapEditor.StateTracker.Segment;
using Track;
using UnityEngine;

namespace MapEditor.Managers
{
  public static class NodeManager
  {

    public static void Move(Direction direction, TrackNode? node = null)
    {
      node ??= EditorContext.SelectedNode;
      if (node == null)
      {
        return;
      }

      var vector =
        direction switch
        {
          Direction.up => node.transform.up * Scaling,
          Direction.down => node.transform.up * -Scaling,
          Direction.left => node.transform.right * -Scaling,
          Direction.right => node.transform.right * Scaling,
          Direction.forward => node.transform.forward * Scaling,
          Direction.backward => node.transform.forward * -Scaling,
          _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null!)
        };

      EditorContext.ChangeManager.AddChange(new ChangeTrackNode(node).Move(node.transform.localPosition + vector));
    }

    public static void Rotate(Vector3 offset, TrackNode? node = null)
    {
      node ??= EditorContext.SelectedNode;
      if (node == null)
      {
        return;
      }

      EditorContext.ChangeManager.AddChange(new ChangeTrackNode(node).Rotate(node.transform.localEulerAngles + offset * Scaling));
    }

    public static bool GetFlipSwitchStand(TrackNode? node = null)
    {
      node ??= EditorContext.SelectedNode;
      return node != null && node.flipSwitchStand;
    }

    public static void FlipSwitchStand(bool value, TrackNode? node = null)
    {
      node ??= EditorContext.SelectedNode;
      if (node == null || node.flipSwitchStand == value)
      {
        return;
      }
      EditorContext.ChangeManager.AddChange(new ChangeTrackNode(node).FlipSwitchStand(value));
    }

    #region Scaling

    public static float Scaling { get; set; } = 1.0f;

    public static void MultiplyScaling()
    {
      if (Scaling <= 10)
      {
        Scaling *= 10;
      }
    }

    public static void DivideScaling()
    {
      if (Scaling > 0.01f)
      {
        Scaling /= 10;
      }
    }

    #endregion

    public static void AddNode(TrackNode? node = null)
    {
      node ??= EditorContext.SelectedNode!;
      var nid = EditorContext.TrackNodeIdGenerator.Next()!;
      var sid = EditorContext.TrackSegmentIdGenerator.Next()!;
      EditorContext.ChangeManager.AddChange(new CompoundChange(
        new CreateTrackNode(nid, node.transform.localPosition + node.transform.forward * Scaling, node.transform.localEulerAngles),
        new CreateTrackSegment(sid, node.id, nid)
      ));
      var newNode = Graph.Shared.GetNode(nid);
      EditorContext.SelectedNode = newNode;

      Rebuild();
    }

    public static void ConnectNodes(string previousNodeId)
    {
      var sid = EditorContext.TrackSegmentIdGenerator.Next()!;
      var segmentCreated = new CreateTrackSegment(sid, previousNodeId, EditorContext.SelectedNode!.id);
      EditorContext.ChangeManager.AddChange(segmentCreated);

      Rebuild();
    }

    public static void SplitNode(TrackNode? node = null)
    {
      node ??= EditorContext.SelectedNode;
      if (node == null)
      {
        return;
      }
      // simple track node split:
      // NODE_A --- NODE --- NODE_B
      // result:
      // NODE_A --- NODE
      //            NEW_NODE --- NODE_B

      // switch node split:
      // NODE_A ---\
      //            >- NODE --- NODE_C
      // NODE_B ---/
      // result:
      // NODE_A --- NODE
      // NODE_B --- NEW_NODE_1
      //            NEW_NODE_2 --- NODE_C

      var actions = new List<IUndoable>();

      var segments = Graph.Shared.SegmentsAffectedByNodes([node])!;
      segments.Remove(segments.First());
      if (segments.Count == 0)
      {
        return;
      }

      foreach (var trackSegment in segments)
      {
        actions.Add(new DeleteTrackSegment(trackSegment));
      }

      foreach (var trackSegment in segments)
      {
        var other = trackSegment.GetOtherNode(node)!;

        // move new node in direction of 'other' node (result should be then nodes not on top of each other with buffer stops on each end)
        var offset = (other.transform.localPosition - node.transform.localPosition).normalized * 3f;

        var nid = EditorContext.TrackNodeIdGenerator.Next()!;
        var sid = EditorContext.TrackSegmentIdGenerator.Next()!;
        actions.Add(new CreateTrackNode(nid, node.transform.localPosition + offset, node.transform.localEulerAngles));
        actions.Add(new CreateTrackSegment(sid, other.id, nid));
      }

      EditorContext.ChangeManager.AddChange(new CompoundChange(actions));

      Rebuild();
    }

    public static void RemoveNode(bool altMode, TrackNode? node = null)
    {
      node ??= EditorContext.SelectedNode;
      if (node == null)
      {
        return;
      }
      // end track node remove:
      // NODE_A --- NODE
      // result
      // NODE_A

      // simple track node remove:
      // NODE_A --- NODE --- NODE_B
      // result:
      // NODE_A     NODE_B
      // result (alt):
      // NODE_A --- NODE_B

      // switch track node remove:
      // switch node split:
      // NODE_A ---\
      //            >- NODE --- NODE_C
      // NODE_B ---/
      // result:
      // NODE_A
      //               NODE_C
      // NODE_B

      EditorContext.SelectedNode = null;

      var actions = new List<IUndoable>();

      var segments = Graph.Shared.SegmentsAffectedByNodes([node])!;

      foreach (var trackSegment in segments)
      {
        actions.Add(new DeleteTrackSegment(trackSegment));
      }

      actions.Add(new DeleteTrackNode(node));

      if (segments.Count == 2 && altMode)
      {
        var firstSegment = segments.First();
        var firstNode = firstSegment.GetOtherNode(node)!;
        var lastNode = segments.Last().GetOtherNode(node)!;

        var sid = EditorContext.TrackSegmentIdGenerator.Next()!;
        actions.Add(new CreateTrackSegment(sid, firstNode.id, lastNode.id, firstSegment.priority, firstSegment.speedLimit, firstSegment.groupId!, firstSegment.style, firstSegment.trackClass));
      }

      EditorContext.ChangeManager.AddChange(new CompoundChange(actions));

      Rebuild();
    }

    private static readonly Serilog.ILogger _Logger =  Serilog.Log.ForContext(typeof(NodeManager));
    public static void InjectNode(TrackSegment? trackSegment = null)
    {
      // inject node in center of segment:
      // NODE_A  --- NODE_B
      // result:
      // NODE_A  --- NODE --- NODE_B

      trackSegment ??= EditorContext.SelectedSegment;
      if (trackSegment == null)
      {
        return;
      }

      _Logger.Information("\r\n\r\nInjectNode START");

      var nodeA = trackSegment.a.id;
      var nodeB = trackSegment.b.id;

      var position = trackSegment.Curve.GetPoint(0.5f);
      var eulerAngles = trackSegment.Curve.GetRotation(0.5f).eulerAngles;

      EditorContext.SelectedSegment = null;

      var nid = EditorContext.TrackNodeIdGenerator.Next()!;
      var sid1 = EditorContext.TrackSegmentIdGenerator.Next()!;
      var sid2 = EditorContext.TrackSegmentIdGenerator.Next()!;

      var actions = new List<IUndoable>
      {
        new DeleteTrackSegment(trackSegment),
        new CreateTrackNode(nid, position, eulerAngles),
        new CreateTrackSegment(sid1, nodeA, nid),
        new CreateTrackSegment(sid2, nid, nodeB),
      };

      EditorContext.ChangeManager.AddChange(new CompoundChange(actions));
      EditorContext.SelectedNode = Graph.Shared.GetNode(nid);
      EditorContext.MoveCameraToSelectedNode();
      Rebuild();
    }

    #region Rotation

    private static Vector3 _savedRotation = Vector3.forward;

    public static void CopyNodeRotation()
    {
      var node = EditorContext.SelectedNode!;
      _savedRotation = node.transform.localEulerAngles;
    }

    public static void PasteNodeRotation()
    {
      var node = EditorContext.SelectedNode!;
      EditorContext.ChangeManager.AddChange(new ChangeTrackNode(node).Rotate(_savedRotation));

      Rebuild();
    }

    #endregion

    #region Elevation

    private static float _savedElevation;

    public static void CopyNodeElevation()
    {
      var node = EditorContext.SelectedNode!;
      _savedElevation = node.transform.localPosition.y;
    }

    public static void PasteNodeElevation()
    {
      var node = EditorContext.SelectedNode!;
      EditorContext.ChangeManager.AddChange(new ChangeTrackNode(node).Move(y: _savedElevation));

      Rebuild();
    }

    #endregion

    private static void Rebuild()
    {
      // not sure why this is not working, but calling same method from 'Rebuild Track' button works ...
      Graph.Shared.RebuildCollections();
      TrackObjectManager.Instance.Rebuild();
    }

  }
}

public enum Direction
{

  up,
  down,
  left,
  right,
  forward,
  backward

}
