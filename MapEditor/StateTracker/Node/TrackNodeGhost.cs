using System.Collections.Generic;
using MapEditor.Extensions;
using MapEditor.Helpers;
using Track;
using UnityEngine;

namespace MapEditor.StateTracker.Node
{
  public sealed class TrackNodeGhost(string id)
  {

    internal Vector3 _Position;
    internal Vector3 _Rotation;
    internal bool _FlipSwitchStand;

    public TrackNodeGhost(string id, Vector3 position, Vector3 rotation, bool flipSwitchStand)
      : this(id)
    {
      _Position = position;
      _Rotation = rotation;
      _FlipSwitchStand = flipSwitchStand;
    }

    public void UpdateGhost(TrackNode node)
    {
      _Position = node.transform.localPosition;
      _Rotation = node.transform.localEulerAngles;
      _FlipSwitchStand = node.flipSwitchStand;
    }

    public void UpdateNode(TrackNode node)
    {
      node.transform.localPosition = _Position;
      node.transform.localEulerAngles = _Rotation;
      node.flipSwitchStand = _FlipSwitchStand;
      Graph.Shared.OnNodeDidChange(node);
      var nodes = new HashSet<TrackNode> { node };
      var segments = Graph.Shared.SegmentsConnectedTo(node);
      foreach (var segment in segments) {
        nodes.Add(segment.a);
        nodes.Add(segment.b);
      }
      foreach (var n in nodes) {
        if (Graph.Shared.IsSwitch(n)) {
          var switchSegments = Graph.Shared.SegmentsConnectedTo(n);
          foreach (var segment in segments) {
            nodes.Add(segment.a);
            nodes.Add(segment.b);
          }
        }
      }
      foreach (var n in nodes) {
        //Graph.Shared.OnNodeDidChange(n);
        TrackObjectManager.Instance.SetNeedsRebuild(n);
        var helper = n.GetComponentInChildren<TrackNodeHelper>();
        if (helper != null) {
          helper.SwitchHelper();
        }
      }
    }

    public void CreateNode()
    {
      var node = Graph.Shared.AddNode(id, _Position, Quaternion.Euler(_Rotation));
      node.transform.SetParent(Graph.Shared.transform);
      // var gameObject = new GameObject($"Node {id}");
      // gameObject.SetActive(false);
      // var node = gameObject.AddComponent<TrackNode>();
      // node.id = id;
      // node.transform.SetParent(Graph.Shared.transform);
      UpdateNode(node);
      // gameObject.SetActive(true);
      // Graph.Shared.AddNode(node);
      EditorContext.PatchEditor!.AddOrUpdateNode(node);
      EditorContext.AttachUiHelper(node);
    }

    public void DestroyNode()
    {
      var node = Graph.Shared.GetNode(id)!;
      UpdateGhost(node);
      Object.Destroy(node.gameObject);
      Graph.Shared.RebuildCollections();
      EditorContext.PatchEditor!.RemoveNode(id);
    }

  }
}
