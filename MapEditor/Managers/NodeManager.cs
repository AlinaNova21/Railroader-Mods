using System;
using System.Collections.Generic;
using System.Linq;
using MapEditor.StateTracker;
using MapEditor.StateTracker.Node;
using MapEditor.StateTracker.Segment;
using Serilog;
using Track;
using UnityEngine;

namespace MapEditor.Managers
{
  public static class NodeManager
  {

    private static Serilog.ILogger _logger = Log.ForContext(typeof(NodeManager))!;

    private static EditorContext Context => EditorContext.Instance;

    public static void Move(Direction direction)
    {
      var node = Context.SelectedNode;
      if (node == null)
      {
        return;
      }

      var vector =
        direction switch
        {
          Direction.up       => node.transform.up * Scaling,
          Direction.down     => node.transform.up * -Scaling,
          Direction.left     => node.transform.right * -Scaling,
          Direction.right    => node.transform.right * Scaling,
          Direction.forward  => node.transform.forward * Scaling,
          Direction.backward => node.transform.forward * -Scaling,
          _                  => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };

      Context.ChangeManager.AddChange(new ChangeTrackNode(node).Move(node.transform.localPosition + vector));
    }

    public static void Rotate(Vector3 offset)
    {
      var node = Context.SelectedNode;
      if (node == null)
      {
        return;
      }

      Context.ChangeManager.AddChange(new ChangeTrackNode(node).Rotate(node.transform.localEulerAngles + offset * Scaling));
    }

    public static void FlipSwitchStand()
    {
      var node = Context.SelectedNode;
      if (node == null)
      {
        return;
      }

      Context.ChangeManager.AddChange(new ChangeTrackNode(node).FlipSwitchStand());
    }

    #region Scaling

    public static float Scaling { get; private set; } = 1.0f;
    public static float ScalingDelta { get; set; } = 1.0f;

    public static void IncrementScaling()
    {
      Scaling += ScalingDelta;
    }

    public static void ResetScaling()
    {
      Scaling = 0;
    }

    public static void DecrementScaling()
    {
      Scaling = Math.Max(0, Scaling - ScalingDelta);
    }

    public static void MultiplyScalingDelta()
    {
      ScalingDelta *= 10;
    }

    public static void DivideScalingDelta()
    {
      if (ScalingDelta > 0.01f)
      {
        ScalingDelta /= 10;
      }
    }

    #endregion

    public static void AddNode()
    {
      var node = Context.SelectedNode;
      var nid = Context.TrackNodeIdGenerator.Next();
      var sid = Context.TrackSegmentIdGenerator.Next();
      Context.ChangeManager.AddChange(new CompoundChange(
        new CreateTrackNode(nid, node.transform.localPosition + node.transform.forward * Scaling, node.transform.localEulerAngles),
        new CreateTrackSegment(sid, node.id, nid)
      ));
      var newNode = Graph.Shared.GetNode(nid);
      Context.SelectNode(newNode);

      Rebuild();
    }

    public static void ConnectNodes(string previousNodeId)
    {
      var sid = Context.TrackSegmentIdGenerator.Next();
      var segmentCreated = new CreateTrackSegment(sid, previousNodeId, Context.SelectedNode.id);
      Context.ChangeManager.AddChange(segmentCreated);

      Rebuild();
    }

    public static void SplitNode()
    {
      // simple track node split:
      // NODE_A --- NODE --- NODE_B
      // result:
      // NODE_A --- NEW_NODE_1
      //            NEW_NODE_2 --- NODE_B

      // switch node split:
      // NODE_A ---\
      //            >- NODE --- NODE_C
      // NODE_B ---/
      // result:
      // NODE_A --- NEW_NODE_1
      // NODE_B --- NEW_NODE_2
      //            NEW_NODE_3 --- NODE_C
      var node = Context.SelectedNode;
      Context.SelectNode(null);

      var actions = new List<IUndoable>();

      var segments = Graph.Shared.SegmentsAffectedByNodes(new HashSet<TrackNode> { node });
      foreach (var trackSegment in segments)
      {
        actions.Add(new DeleteTrackSegment(trackSegment));
      }

      actions.Add(new DeleteTrackNode(node));

      foreach (var trackSegment in segments)
      {
        var other = trackSegment.GetOtherNode(node);

        // move new node in direction of 'other' node (result should be then nodes not on top of each other with buffer stops on each end)
        var offset = (other.transform.localPosition - node.transform.localPosition).normalized * 1.5f;

        var nid = Context.TrackNodeIdGenerator.Next();
        var sid = Context.TrackSegmentIdGenerator.Next();
        actions.Add(new CreateTrackNode(nid, node.transform.localPosition + offset, node.transform.localEulerAngles));
        actions.Add(new CreateTrackSegment(sid, other.id, nid));
      }

      Context.ChangeManager.AddChange(new CompoundChange(actions));

      Rebuild();
    }

    public static void RemoveNode(bool altMode)
    {
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

      var node = Context.SelectedNode;
      Context.SelectNode(null);

      var actions = new List<IUndoable>();

      var segments = Graph.Shared.SegmentsAffectedByNodes(new HashSet<TrackNode> { node });

      foreach (var trackSegment in segments)
      {
        actions.Add(new DeleteTrackSegment(trackSegment));
      }

      actions.Add(new DeleteTrackNode(node));

      if (segments.Count == 2 && altMode)
      {
        var firstSegment = segments.First();
        var firstNode = firstSegment.GetOtherNode(node);
        var lastNode = segments.Last().GetOtherNode(node);

        var sid = Context.TrackSegmentIdGenerator.Next();
        actions.Add(new CreateTrackSegment(sid, firstNode.id, lastNode.id, firstSegment.priority, firstSegment.speedLimit, firstSegment.groupId, firstSegment.style, firstSegment.trackClass));
      }

      Context.ChangeManager.AddChange(new CompoundChange(actions));

      Rebuild();
    }

   
    #region Rotation

    private static Vector3 _savedRotation = Vector3.forward;

    public static void CopyNodeRotation() {
      var node = Context.SelectedNode;
      _savedRotation = node.transform.localEulerAngles;
    }

    public static void PasteNodeRotation() {
      var node = Context.SelectedNode;
      Context.ChangeManager.AddChange(new ChangeTrackNode(node).Rotate(_savedRotation));

      Rebuild();
    }

    #endregion

    #region Elevation

    private static float _savedElevation = 0;

    public static void CopyNodeElevation() {
      var node = Context.SelectedNode;
      _savedElevation = node.transform.localPosition.y;
    }

    public static void PasteNodeElevation() {
      var node = Context.SelectedNode;
      Context.ChangeManager.AddChange(new ChangeTrackNode(node).Move(y: _savedElevation));

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
